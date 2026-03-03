using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Application.Services
{
    public class ClaimService : IClaimService
    {
        private readonly IClaimRepository _claimRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly IPaymentRepository _paymentRepository;

        public ClaimService(
            IClaimRepository claimRepository,
            IPolicyRepository policyRepository,
            IUserRepository userRepository,
            IDocumentRepository documentRepository,
            INotificationService notificationService,
            IEmailService emailService,
            IWebHostEnvironment environment,
            IPaymentRepository paymentRepository)
        {
            _claimRepository = claimRepository;
            _policyRepository = policyRepository;
            _userRepository = userRepository;
            _documentRepository = documentRepository;
            _notificationService = notificationService;
            _emailService = emailService;
            _environment = environment;
            _paymentRepository = paymentRepository;
        }

        // ── Scenario A — Customer files Death Claim ───────────
        public async Task<ClaimResponseDto> FileClaimAsync(
            int customerId, FileClaimDto dto)
        {
            var policy = await _policyRepository
      .GetByIdWithDetailsAsync(dto.PolicyAssignmentId);

            if (policy == null)
                throw new NotFoundException("Policy", dto.PolicyAssignmentId);

            if (policy.CustomerId != customerId)
                throw new ForbiddenException(
                    "You can only file claims for your own policies");

            // ── Policy status checks ──────────────────────────────
            if (policy.Status == PolicyStatus.Closed)
                throw new BadRequestException(
                    "A claim has already been settled for this policy");

            if (policy.Status == PolicyStatus.Matured)
                throw new BadRequestException(
                    "This policy has already matured");

            if (policy.Status != PolicyStatus.Active)
                throw new BadRequestException(
                    "Claims can only be filed for active policies");

            // ── Maturity claim block ──────────────────────────────
            if (dto.ClaimType == ClaimType.Maturity)
                throw new BadRequestException(
                    "Maturity claims are processed automatically " +
                    "by the system when policy end date is reached");

            // ── Payment check ─────────────────────────────────────
            var hasPaid = await _paymentRepository
                .HasAnyCompletedPaymentAsync(dto.PolicyAssignmentId);

            if (!hasPaid)
                throw new BadRequestException(
                    "At least one premium must be paid before filing a claim");

            // ── Grace period check ────────────────────────────────
            if (DateTime.UtcNow > policy.NextDueDate
                    .AddDays(policy.Plan!.GracePeriodDays))
                throw new BadRequestException(
                    "Policy has lapsed due to non-payment beyond grace period");

            var isInGracePeriod = DateTime.UtcNow > policy.NextDueDate &&
                                  DateTime.UtcNow <= policy.NextDueDate
                                      .AddDays(policy.Plan.GracePeriodDays);

            if (DateTime.UtcNow.Date > policy.EndDate.Date)
                throw new BadRequestException(
                    "Cannot file a claim after policy end date");

            // ── Member validation ─────────────────────────────────
            var member = policy.PolicyMembers?
                .FirstOrDefault(m => m.Id == dto.PolicyMemberId);

            if (member == null)
                throw new BadRequestException(
                    "Policy member does not belong to this policy");

            // If primary insured is dying, all other active claims
            // on other members should be resolved first
            if (member.IsPrimaryInsured)
            {
                var otherActiveMembers = policy.PolicyMembers?
                    .Where(m => m.Id != member.Id)
                    .ToList();

                foreach (var otherMember in otherActiveMembers ?? new())
                {
                    var otherHasActiveClaim = await _claimRepository
                        .HasActiveclaimAsync(otherMember.Id);

                    if (otherHasActiveClaim)
                        throw new BadRequestException(
                            "Cannot file a claim for the primary insured while " +
                            "other members have active pending claims");
                }
            }

            // ── Active claim check ────────────────────────────────
            var hasActiveClaim = await _claimRepository
                .HasActiveclaimAsync(dto.PolicyMemberId);

            if (hasActiveClaim)
                throw new ConflictException(
                    "An active claim already exists for this member");

            // ── Death claim specific validations ──────────────────
            if (dto.ClaimType == ClaimType.Death)
            {
                if (string.IsNullOrWhiteSpace(dto.DeathCertificateNumber))
                    throw new BadRequestException(
                        "Death certificate number is required for death claims");

                if (string.IsNullOrWhiteSpace(dto.NomineeName))
                    throw new BadRequestException(
                        "Nominee name is required for death claims");

                if (string.IsNullOrWhiteSpace(dto.NomineeContact))
                    throw new BadRequestException(
                        "Nominee contact is required for death claims");

                if (dto.Documents == null || !dto.Documents.Any())
                    throw new BadRequestException(
                        "Supporting documents are required for death claims");
            }

            // Auto-populate Nominee details from policy if not provided
            var nomineeName = dto.NomineeName;
            var nomineeContact = dto.NomineeContact;

            if (dto.ClaimType == ClaimType.Death && string.IsNullOrWhiteSpace(nomineeName))
            {
                var nominees = policy.PolicyNominees?.ToList() ?? new();
                if (nominees.Any())
                {
                    nomineeName = string.Join(", ", nominees.Select(n => $"{n.NomineeName} ({n.SharePercentage}%)"));
                    nomineeContact = string.Join(", ", nominees.Select(n => n.ContactNumber).Distinct());
                }
                else
                {
                    // Fallback to customer if no nominees (though there should be)
                    nomineeName = policy.Customer?.Name ?? "Policy Holder";
                    nomineeContact = policy.Customer?.Phone ?? "N/A";
                }
            }

            var claim = new InsuranceClaim
            {
                PolicyAssignmentId = dto.PolicyAssignmentId,
                PolicyMemberId = dto.PolicyMemberId,
                ClaimsOfficerId = null,
                ClaimType = dto.ClaimType,
                ClaimAmount = member.CoverageAmount,
                NomineeName = nomineeName ?? string.Empty,
                NomineeContact = nomineeContact ?? string.Empty,
                DeathCertificateNumber = dto.DeathCertificateNumber,
                FiledDate = DateTime.UtcNow,
                Status = ClaimStatus.Submitted,
                Remarks = isInGracePeriod
                    ? $"{dto.Remarks} [Filed during grace period]"
                    : dto.Remarks,
                CreatedAt = DateTime.UtcNow
            };

            await _claimRepository.AddAsync(claim);
            await _claimRepository.SaveChangesAsync();

            // Save claim documents
            if (dto.Documents != null && dto.Documents.Any())
                await SaveClaimDocumentsAsync(
                    dto.Documents, claim.Id, customerId);

            // Notify admin
            var admins = await _userRepository.GetByRoleAsync(UserRole.Admin);
            foreach (var admin in admins)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: admin.Id,
                    title: "New Claim Filed",
                    message: $"A new {dto.ClaimType} claim has been filed " +
                             $"for policy {policy.PolicyNumber}",
                    type: NotificationType.ClaimStatusUpdate,
                    policyId: null,
                    claimId: claim.Id,
                    paymentId: null);
            }

            var created = await _claimRepository.GetByIdWithDetailsAsync(claim.Id);
            return MapToDto(created!);
        }

        // ── Admin assigns ClaimsOfficer ───────────────────────
        public async Task AssignClaimsOfficerAsync(
            int claimId, AssignClaimsOfficerDto dto)
        {
            var claim = await _claimRepository.GetByIdWithDetailsAsync(claimId);
            if (claim == null)
                throw new NotFoundException("Claim", claimId);

            var officer = await _userRepository.GetByIdAsync(dto.ClaimsOfficerId);
            if (officer == null || officer.Role != UserRole.ClaimsOfficer)
                throw new BadRequestException(
                    "Provided user is not a valid claims officer");

            claim.ClaimsOfficerId = dto.ClaimsOfficerId;
            claim.Status = ClaimStatus.UnderReview;

            _claimRepository.Update(claim);
            await _claimRepository.SaveChangesAsync();

            // Notify customer
            await _notificationService.CreateNotificationAsync(
                userId: claim.PolicyAssignment!.CustomerId,
                title: "Claim Under Review",
                message: $"Your claim is now under review by {officer.Name}",
                type: NotificationType.ClaimStatusUpdate,
                policyId: null,
                claimId: claim.Id,
                paymentId: null);

            // Notify claims officer
            await _notificationService.CreateNotificationAsync(
                userId: dto.ClaimsOfficerId,
                title: "New Claim Assigned",
                message: $"A new claim has been assigned to you for review. " +
                         $"Policy: {claim.PolicyAssignment.PolicyNumber}",
                type: NotificationType.ClaimStatusUpdate,
                policyId: null,
                claimId: claim.Id,
                paymentId: null);
        }

        // ── ClaimsOfficer processes claim ─────────────────────
        public async Task<ClaimResponseDto> ProcessClaimAsync(
            int claimId, int officerId, ProcessClaimDto dto)
        {
            var claim = await _claimRepository.GetByIdWithDetailsAsync(claimId);
            if (claim == null)
                throw new NotFoundException("Claim", claimId);

            // Removed restrictive guard: "Status must be either Approved or Rejected"
            // We now allow all statuses for full flexibility as requested.

            if (dto.Status == ClaimStatus.Approved || dto.Status == ClaimStatus.Settled)
            {
                if (dto.SettlementAmount == null || dto.SettlementAmount <= 0)
                    throw new BadRequestException(
                        "Settlement amount is required when approving or settling a claim");

                if (dto.SettlementAmount > claim.ClaimAmount)
                    throw new BadRequestException(
                        $"Settlement cannot exceed ₹{claim.ClaimAmount:N2}");

                claim.SettlementAmount = dto.SettlementAmount;

                // Update policy status to Closed if settled/approved
                var policy = await _policyRepository.GetByIdAsync(claim.PolicyAssignmentId);
                if (policy != null)
                {
                    policy.Status = PolicyStatus.Closed;
                    _policyRepository.Update(policy);
                }
            }
            else if (dto.Status == ClaimStatus.Rejected)
            {
                claim.SettlementAmount = 0;
            }

            claim.Status = dto.Status;
            claim.Remarks = dto.Remarks;
            claim.ProcessedDate = DateTime.UtcNow;

            _claimRepository.Update(claim);

            claim.Remarks = dto.Remarks;
            claim.ProcessedDate = DateTime.UtcNow;

            _claimRepository.Update(claim);

            // ── Save claim first, then policy ────────────────────
            await _claimRepository.SaveChangesAsync();      // ← claim saved
            await _policyRepository.SaveChangesAsync();     // ← policy saved separately

            // ── Fetch fresh from DB after save ───────────────────
            var updated = await _claimRepository.GetByIdWithDetailsAsync(claimId);

            // Notify and email
            var customer = await _userRepository
                .GetByIdAsync(claim.PolicyAssignment!.CustomerId);

            var statusMessage = claim.Status == ClaimStatus.Settled
                ? $"Your claim has been approved. " +
                  $"Settlement: ₹{claim.SettlementAmount:N2}"
                : $"Your claim has been rejected. Remarks: {dto.Remarks}";

            await _notificationService.CreateNotificationAsync(
                userId: claim.PolicyAssignment.CustomerId,
                title: $"Claim {claim.Status}",
                message: statusMessage,
                type: NotificationType.ClaimStatusUpdate,
                policyId: null,
                claimId: claim.Id,
                paymentId: null);

            await _emailService.SendPolicyStatusChangedAsync(
                customer!.Email,
                customer.Name,
                claim.PolicyAssignment.PolicyNumber,
                claim.Status.ToString());

            return MapToDto(updated!);
        }

        // ── Scenario B — Maturity (Background Service) ────────
        public async Task ProcessMaturityClaimsAsync()
        {
            var maturedPolicies = await _policyRepository
                .GetMaturedPoliciesAsync();

            foreach (var policy in maturedPolicies)
            {
                // Check if maturity benefit applies
                if (!policy.Plan!.HasMaturityBenefit)
                {
                    // No benefit — just expire the policy
                    policy.Status = PolicyStatus.Expired;
                    _policyRepository.Update(policy);
                    await _policyRepository.SaveChangesAsync();
                    continue;
                }

                // Create automatic maturity claim
                var primaryMember = policy.PolicyMembers?
                    .FirstOrDefault(m => m.IsPrimaryInsured);

                if (primaryMember == null) continue;

                // Check no claim already exists
                var hasActiveClaim = await _claimRepository
                    .HasActiveclaimAsync(primaryMember.Id);

                if (hasActiveClaim) continue;

                var maturityClaim = new InsuranceClaim
                {
                    PolicyAssignmentId = policy.Id,
                    PolicyMemberId = primaryMember.Id,
                    ClaimType = ClaimType.Maturity,
                    ClaimAmount = primaryMember.CoverageAmount,
                    NomineeName = policy.Customer?.Name ?? string.Empty,
                    NomineeContact = policy.Customer?.Phone ?? string.Empty,
                    FiledDate = DateTime.UtcNow,
                    Status = ClaimStatus.Approved,
                    SettlementAmount = primaryMember.CoverageAmount,
                    ProcessedDate = DateTime.UtcNow,
                    Remarks = "Auto-processed maturity benefit",
                    CreatedAt = DateTime.UtcNow
                };

                await _claimRepository.AddAsync(maturityClaim);

                // Move policy to Matured
                policy.Status = PolicyStatus.Matured;
                _policyRepository.Update(policy);

                await _claimRepository.SaveChangesAsync();
                await _policyRepository.SaveChangesAsync();

                // Notify customer
                await _notificationService.CreateNotificationAsync(
                    userId: policy.CustomerId,
                    title: "Policy Matured — Benefit Credited",
                    message: $"Your policy {policy.PolicyNumber} has matured. " +
                             $"Maturity benefit of " +
                             $"₹{primaryMember.CoverageAmount:N2} will be credited.",
                    type: NotificationType.PolicyStatusUpdate,
                    policyId: null,
                    claimId: maturityClaim.Id,
                    paymentId: null);

                // Email customer
                await _emailService.SendPolicyStatusChangedAsync(
                    policy.Customer!.Email,
                    policy.Customer.Name,
                    policy.PolicyNumber,
                    "Matured");
            }
        }

        // ── Getters ───────────────────────────────────────────
        public async Task<IEnumerable<ClaimResponseDto>> GetAllClaimsAsync()
        {
            var claims = await _claimRepository.GetAllAsync();
            return claims.Select(MapToDto);
        }

        public async Task<IEnumerable<ClaimResponseDto>> GetMyClaimsAsync(
            int customerId)
        {
            var claims = await _claimRepository.GetByCustomerIdAsync(customerId);
            return claims.Select(MapToDto);
        }

        public async Task<IEnumerable<ClaimResponseDto>> GetMyAssignedClaimsAsync(
            int officerId)
        {
            var claims = await _claimRepository
                .GetByClaimsOfficerIdAsync(officerId);
            return claims.Select(MapToDto);
        }

        public async Task<ClaimResponseDto> GetClaimByIdAsync(int id)
        {
            var claim = await _claimRepository.GetByIdWithDetailsAsync(id);
            if (claim == null)
                throw new NotFoundException("Claim", id);
            return MapToDto(claim);
        }

        // ── Private Helpers ───────────────────────────────────
        private async Task SaveClaimDocumentsAsync(
            List<IFormFile> files, int claimId, int uploadedByUserId)
        {
            var folderPath = Path.Combine(
                _environment.WebRootPath,
                "uploads", "claims", claimId.ToString());

            Directory.CreateDirectory(folderPath);

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.FileName);
                var uniqueName = $"ClaimDoc_{claimId}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(folderPath, uniqueName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                var document = new Document
                {
                    FileName = uniqueName,
                    FilePath = $"uploads/claims/{claimId}/{uniqueName}",
                    DocumentCategory = "ClaimDocument",
                    UploadedAt = DateTime.UtcNow,
                    UploadedByUserId = uploadedByUserId,
                    ClaimId = claimId,
                    PolicyAssignmentId = null
                };

                await _documentRepository.AddAsync(document);
            }

            await _documentRepository.SaveChangesAsync();
        }

        private static ClaimResponseDto MapToDto(InsuranceClaim c)
        {
            var dto = new ClaimResponseDto
            {
                Id = c.Id,
                PolicyAssignmentId = c.PolicyAssignmentId,
                PolicyNumber = c.PolicyAssignment?.PolicyNumber ?? string.Empty,
                PolicyMemberId = c.PolicyMemberId,
                PolicyMemberName = c.PolicyMember?.MemberName ?? string.Empty,
                ClaimsOfficerId = c.ClaimsOfficerId,
                ClaimsOfficerName = c.ClaimsOfficer?.Name,
                ClaimType = c.ClaimType.ToString(),
                ClaimAmount = c.ClaimAmount,
                NomineeName = c.NomineeName,
                NomineeContact = c.NomineeContact,
                DeathCertificateNumber = c.DeathCertificateNumber,
                FiledDate = c.FiledDate,
                Status = c.Status.ToString(),
                Remarks = c.Remarks,
                SettlementAmount = c.SettlementAmount,
                ProcessedDate = c.ProcessedDate,
                CreatedAt = c.CreatedAt,
                Documents = c.Documents?.Select(d => new DocumentResponseDto
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    DocumentCategory = d.DocumentCategory,
                    UploadedAt = d.UploadedAt
                }).ToList() ?? new()
            };

            // Calculate settlement breakdown if settled
            if (c.Status == ClaimStatus.Settled && c.SettlementAmount > 0 && c.PolicyAssignment?.PolicyNominees != null)
            {
                dto.SettlementBreakdown = c.PolicyAssignment.PolicyNominees.Select(n => new ClaimNomineeSettlementDto
                {
                    NomineeName = n.NomineeName,
                    SharePercentage = n.SharePercentage,
                    SettlementAmount = (c.SettlementAmount.Value * n.SharePercentage) / 100
                }).ToList();
            }

            return dto;
        }
    }
}