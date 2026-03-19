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
        private readonly ILoanRepository _loanRepository;
        private readonly IPolicyService _policyService;
        private readonly ISystemConfigRepository _systemConfigRepository;

        public ClaimService(
            IClaimRepository claimRepository,
            IPolicyRepository policyRepository,
            IUserRepository userRepository,
            IDocumentRepository documentRepository,
            INotificationService notificationService,
            IEmailService emailService,
            IWebHostEnvironment environment,
            IPaymentRepository paymentRepository,
            ILoanRepository loanRepository,
            IPolicyService policyService,
            ISystemConfigRepository systemConfigRepository)
        {
            _claimRepository = claimRepository;
            _policyRepository = policyRepository;
            _userRepository = userRepository;
            _documentRepository = documentRepository;
            _notificationService = notificationService;
            _emailService = emailService;
            _environment = environment;
            _paymentRepository = paymentRepository;
            _loanRepository = loanRepository;
            _policyService = policyService;
            _systemConfigRepository = systemConfigRepository;
        }

        // Customer files Death Claim 
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

            // Policy status checks 
            if (policy.Status == PolicyStatus.Lapsed)
                throw new BadRequestException(
                    "Claims cannot be filed on a lapsed policy.");

            if (policy.Status == PolicyStatus.Closed)
                throw new BadRequestException(
                    "A claim has already been settled for this policy");

            if (policy.Status == PolicyStatus.Matured)
                throw new BadRequestException(
                    "This policy has already matured");

            if (policy.Status != PolicyStatus.Active)
                throw new BadRequestException(
                    "Claims can only be filed for active policies");

            //  Maturity claim block 
            if (dto.ClaimType == ClaimType.Maturity)
                throw new BadRequestException(
                    "Maturity claims are processed automatically " +
                    "by the system when policy end date is reached");

            // Payment check 
            var hasPaid = await _paymentRepository
                .HasAnyCompletedPaymentAsync(dto.PolicyAssignmentId);

            if (!hasPaid)
                throw new BadRequestException(
                    "At least one premium must be paid before filing a claim");

            //  Grace period check 
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

            //  Member validation 
            var member = policy.PolicyMembers?
                .FirstOrDefault(m => m.Id == dto.ClaimForMemberId);

            if (member == null)
                throw new BadRequestException(
                    "Policy member does not belong to this policy");

            // Prevent duplicate death claims or claims for inactive members
            if (member.Status != MemberStatus.Active)
            {
                var statusMsg = member.Status == MemberStatus.Deceased 
                    ? "A death claim has already been settled for this policy member"
                    : $"Cannot file claim for member with status: {member.Status}";
                
                throw new BadRequestException(statusMsg);
            }

            // Active claim check 
            var hasActiveClaim = await _claimRepository
                .HasActiveclaimAsync(dto.ClaimForMemberId);

            if (hasActiveClaim)
                throw new ConflictException(
                    "An active claim already exists for this member");

            // Death claim specific validations 
            if (dto.ClaimType == ClaimType.Death)
            {
                if (string.IsNullOrWhiteSpace(dto.DeathCertificateNumber))
                    throw new BadRequestException(
                        "Death certificate number is required for death claims");

                if (dto.Documents == null || !dto.Documents.Any())
                    throw new BadRequestException(
                        "Supporting documents are required for death claims");
            }

            // Automatically add Nominee details from policy
            string? nomineeName = null;
            string? nomineeContact = null;

            var nominees = policy.PolicyNominees?.ToList() ?? new();
            if (nominees.Any())
            {
                nomineeName = string.Join(", ", nominees.Select(n => $"{n.NomineeName} ({n.SharePercentage}%)"));
                nomineeContact = string.Join(", ", nominees.Select(n => n.ContactNumber).Distinct());
            }
            else
            {
                // Fallback to customer if no nominees
                nomineeName = policy.Customer?.Name ?? "Policy Holder";
                nomineeContact = policy.Customer?.Phone ?? "N/A";
            }

            var currentCoverage = _policyService.GetCurrentCoverage(policy, member);
            var bonusDetails = _policyService.GetBonusDetails(policy, member);

            // Total claim = coverage + bonus
            var totalClaimAmount = Math.Round(currentCoverage + bonusDetails.TotalBonus, 2);

            // Round Robin assignment for Claims Officer
            int? assignedOfficerId = null;
            var officers = (await _userRepository.GetByRoleAsync(UserRole.ClaimsOfficer))
                            .Where(u => u.IsActive)
                            .OrderBy(u => u.Id)
                            .ToList();

            if (officers.Any())
            {
                var config = await _systemConfigRepository.GetConfigAsync();
                int nextIndex = (config.LastClaimsOfficerIndex + 1) % officers.Count;
                assignedOfficerId = officers[nextIndex].Id;

                config.LastClaimsOfficerIndex = nextIndex;
                _systemConfigRepository.Update(config);
                await _systemConfigRepository.SaveChangesAsync();
            }

            var claim = new InsuranceClaim
            {
                PolicyAssignmentId = dto.PolicyAssignmentId,
                PolicyMemberId = member.Id,
                ClaimForMemberId = member.Id,
                ClaimsOfficerId = assignedOfficerId,
                ClaimType = dto.ClaimType,
                ClaimAmount = totalClaimAmount,
                NomineeName = nomineeName ?? string.Empty,
                NomineeContact = nomineeContact ?? string.Empty,
                DeathCertificateNumber = dto.DeathCertificateNumber,
                DateOfDeath = dto.DateOfDeath,
                CauseOfDeath = dto.CauseOfDeath,
                PlaceOfDeath = dto.PlaceOfDeath,
                FiledDate = DateTime.UtcNow,
                Status = ClaimStatus.Submitted,
                Remarks = $"Coverage: ₹{currentCoverage:N2}" +
                          (bonusDetails.TotalBonus > 0 ? $" + Bonus: ₹{bonusDetails.TotalBonus:N2}" : "") +
                          $" = Total: ₹{totalClaimAmount:N2}" +
                          (isInGracePeriod ? " [Filed during grace period]" : "") +
                          (string.IsNullOrWhiteSpace(dto.Remarks) ? "" : $" | Note: {dto.Remarks}"),
                CreatedAt = DateTime.UtcNow
            };

            await _claimRepository.AddAsync(claim);
            await _claimRepository.SaveChangesAsync();

            // Save claim documents
            if (dto.Documents != null && dto.Documents.Any())
                await SaveClaimDocumentsAsync(
                    dto.Documents, claim.Id, customerId);

            // Notify Admin
            var admins = await _userRepository.GetByRoleAsync(UserRole.Admin);
            foreach (var admin in admins)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: admin.Id,
                    title: "New Claim Filed",
                    message: $"A new {dto.ClaimType} claim has been filed for policy {policy.PolicyNumber}",
                    type: NotificationType.ClaimStatusUpdate,
                    policyId: policy.Id,
                    claimId: claim.Id,
                    paymentId: null);
            }

            // Notify Agent if assigned
            if (policy.AgentId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: policy.AgentId.Value,
                    title: "Claim Filed for Assigned Policy",
                    message: $"A new claim has been filed for policy {policy.PolicyNumber} managed by you.",
                    type: NotificationType.ClaimStatusUpdate,
                    policyId: policy.Id,
                    claimId: claim.Id,
                    paymentId: null);
            }

            // Notify Customer (Verify existing or Add)
            var assignedOfficerName = assignedOfficerId.HasValue 
                ? officers.FirstOrDefault(o => o.Id == assignedOfficerId.Value)?.Name ?? "an officer"
                : "an officer";

            await _notificationService.CreateNotificationAsync(
                userId: customerId,
                title: "Claim Submitted",
                message: $"Your {dto.ClaimType} claim for policy {policy.PolicyNumber} has been received and is being processed by {assignedOfficerName}.",
                type: NotificationType.ClaimStatusUpdate,
                policyId: policy.Id,
                claimId: claim.Id,
                paymentId: null);

            // Notify Claims Officer if assigned
            if (claim.ClaimsOfficerId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: claim.ClaimsOfficerId.Value,
                    title: "New Claim Assigned",
                    message: $"A new {dto.ClaimType} claim for policy {policy.PolicyNumber} has been automatically assigned to you for review.",
                    type: NotificationType.ClaimStatusUpdate,
                    policyId: policy.Id,
                    claimId: claim.Id,
                    paymentId: null);
            }

            var created = await _claimRepository.GetByIdWithDetailsAsync(claim.Id);
            return await MapToDtoAsync(created!);
        }

        // Admin assigns ClaimsOfficer 
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

        // ClaimsOfficer processes claim 
        public async Task<ClaimResponseDto> ProcessClaimAsync(
            int claimId, int officerId, ProcessClaimDto dto)
        {
            var claim = await _claimRepository.GetByIdWithDetailsAsync(claimId);
            if (claim == null)
                throw new NotFoundException("Claim", claimId);

            if (claim.ClaimsOfficerId != officerId)
                throw new ForbiddenException("You are not assigned to this claim");

            // Update verification fields if provided
            if (dto.DateOfDeath.HasValue) claim.DateOfDeath = dto.DateOfDeath;
            if (!string.IsNullOrWhiteSpace(dto.CauseOfDeath)) claim.CauseOfDeath = dto.CauseOfDeath;
            if (!string.IsNullOrWhiteSpace(dto.DeathCertificateNumber)) claim.DeathCertificateNumber = dto.DeathCertificateNumber;

            if (dto.Status == ClaimStatus.Approved || dto.Status == ClaimStatus.Settled)
            {
                if (dto.SettlementAmount == null || dto.SettlementAmount <= 0)
                    throw new BadRequestException(
                        "Settlement amount is required when approving or settling a claim");

                if (dto.SettlementAmount > claim.ClaimAmount)
                    throw new BadRequestException(
                        $"Settlement cannot exceed ₹{claim.ClaimAmount:N2}");

                claim.SettlementAmount = dto.SettlementAmount;

                // Process post-settlement policy & member status updates
                var policy = await _policyRepository.GetByIdWithDetailsAsync(claim.PolicyAssignmentId);
                if (policy != null)
                {
                    if (dto.Status == ClaimStatus.Settled)
                    {
                        var member = policy.PolicyMembers?.FirstOrDefault(m => m.Id == claim.ClaimForMemberId || m.Id == claim.PolicyMemberId);
                        
                        if (member != null && claim.ClaimType == ClaimType.Death)
                        {
                            // 1. Mark member deceased
                            member.Status = MemberStatus.Deceased;
                            
                            // 2. Check remaining active members
                            var remainingActive = policy.PolicyMembers?.Where(m => m.Status == MemberStatus.Active).ToList();
                            
                            if (remainingActive == null || !remainingActive.Any())
                            {
                                // All members deceased -> Policy closed completely
                                policy.Status = PolicyStatus.Closed;
                            }
                            else
                            {
                                // 3. Auto-recalculate ongoing premium
                                // Premium adjusts by removing the deceased member's share of the total coverage
                                var totalCoverage = policy.PolicyMembers?.Sum(m => m.CoverageAmount) ?? 0;
                                var deceasedCoverage = member.CoverageAmount;

                                if (totalCoverage > 0 && totalCoverage > deceasedCoverage)
                                {
                                    var remainingPercentage = (totalCoverage - deceasedCoverage) / totalCoverage;
                                    var oldPremium = policy.TotalPremiumAmount;
                                    policy.TotalPremiumAmount = Math.Round(policy.TotalPremiumAmount * remainingPercentage, 2);

                                    // Trigger premium update notification
                                    if (policy.TotalPremiumAmount < oldPremium)
                                    {
                                    await _notificationService.CreateNotificationAsync(
                                        userId: policy.CustomerId,
                                        title: "Policy Premium Updated",
                                        message: $"Your policy premium has been reduced to ₹{policy.TotalPremiumAmount:N2} following the settlement of claim #{claim.Id}. This change will reflect in your next installment.",
                                        type: NotificationType.PolicyStatusUpdate,
                                        policyId: policy.Id,
                                        claimId: claim.Id,
                                        paymentId: null);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Non-death claim (Maturity) -> fully closes policy
                            policy.Status = PolicyStatus.Closed;
                        }
                    }
                    _policyRepository.Update(policy);

                    if (dto.Status == ClaimStatus.Settled)
                    {
                        var activeLoan = await _loanRepository.GetActiveLoanByPolicyAsync(policy.Id);
                        if (activeLoan != null)
                        {
                            activeLoan.Status = LoanStatus.Adjusted;
                            activeLoan.ClosedDate = DateTime.UtcNow;
                            activeLoan.Remarks = $"Adjusted against claim #{claim.Id} settlement";
                            _loanRepository.Update(activeLoan);
                        }
                    }
                }
            }
            else if (dto.Status == ClaimStatus.Rejected)
            {
                claim.SettlementAmount = 0;
            }

            claim.Status = dto.Status;
            claim.OfficerRemarks = dto.Remarks;
            claim.ProcessedDate = DateTime.UtcNow;

            _claimRepository.Update(claim);

            await _claimRepository.SaveChangesAsync();
            
            // Explicitly save related repositories if they were modified
            if (dto.Status == ClaimStatus.Settled)
            {
                await _policyRepository.SaveChangesAsync();
                await _loanRepository.SaveChangesAsync();
            }

            // Fetch fresh from DB after save 
            var updated = await _claimRepository.GetByIdWithDetailsAsync(claimId);

            try
            {
                // Notify customer (primary notification)
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

                // Notify Claims Officer (primary notification)
                await _notificationService.CreateNotificationAsync(
                    userId: officerId,
                    title: "Claim Processed",
                    message: $"You have successfully processed claim #{claim.Id}.",
                    type: NotificationType.ClaimStatusUpdate,
                    policyId: claim.PolicyAssignmentId,
                    claimId: claim.Id,
                    paymentId: null);
            }
            catch (Exception)
            {
                // Log exception if possible, but don't crash the request since DB is already updated
                // _logger.LogError(ex, "Error sending notifications for claim process");
            }

            return await MapToDtoAsync(updated!);
        }

        // Maturity (Background Service) 
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

                // Calculate bonus details
                var bonusDetails = _policyService.GetBonusDetails(policy, primaryMember);
                var currentCoverage = _policyService.GetCurrentCoverage(policy, primaryMember);

                // Fetch outstanding loan
                var activeLoan = await _loanRepository.GetActiveLoanByPolicyAsync(policy.Id);
                var outstandingLoan = activeLoan?.OutstandingBalance ?? 0;

                // Total payout = SA/CurrentCoverage + Bonus + Terminal Bonus - Loan
                var grossClaimAmount = Math.Round(currentCoverage + bonusDetails.TotalBonus + bonusDetails.TerminalBonus, 2);
                var netSettlement = Math.Round(grossClaimAmount - outstandingLoan, 2);

                var maturityClaim = new InsuranceClaim
                {
                    PolicyAssignmentId = policy.Id,
                    PolicyMemberId = primaryMember.Id,
                    ClaimType = ClaimType.Maturity,
                    ClaimAmount = grossClaimAmount,
                    NomineeName = policy.Customer?.Name ?? string.Empty,
                    NomineeContact = policy.Customer?.Phone ?? string.Empty,
                    FiledDate = DateTime.UtcNow,
                    Status = ClaimStatus.Settled, // Auto-settle matured policies
                    SettlementAmount = netSettlement,
                    ProcessedDate = DateTime.UtcNow,
                    Remarks = $"Auto-processed maturity. Coverage: ₹{currentCoverage:N2} " +
                             $"+ Bonus: ₹{bonusDetails.TotalBonus:N2} " +
                             $"+ Terminal: ₹{bonusDetails.TerminalBonus:N2}" +
                             (outstandingLoan > 0 ? $" - Loan Deduction: ₹{outstandingLoan:N2}" : "") +
                             $" = Net Payout: ₹{netSettlement:N2}",
                    CreatedAt = DateTime.UtcNow
                };

                await _claimRepository.AddAsync(maturityClaim);

                // Move policy to Matured
                policy.Status = PolicyStatus.Matured;
                _policyRepository.Update(policy);

                if (activeLoan != null)
                {
                    activeLoan.Status = LoanStatus.Adjusted;
                    activeLoan.ClosedDate = DateTime.UtcNow;
                    activeLoan.Remarks = "Adjusted against maturity payout";
                    _loanRepository.Update(activeLoan);
                }

                await _loanRepository.SaveChangesAsync();
                await _claimRepository.SaveChangesAsync();
                await _policyRepository.SaveChangesAsync();

                // Notify customer
                await _notificationService.CreateNotificationAsync(
                    userId: policy.CustomerId,
                    title: "Policy Matured — Benefit Credited",
                    message: $"Your policy {policy.PolicyNumber} has matured. " +
                             $"Total payout including bonuses: ₹{maturityClaim.SettlementAmount:N2} credited.",
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

        // Getters 
        public async Task<IEnumerable<ClaimResponseDto>> GetAllClaimsAsync()
        {
            var claims = await _claimRepository.GetAllAsync();
            var dtos = new List<ClaimResponseDto>();
            foreach (var c in claims)
            {
                dtos.Add(await MapToDtoAsync(c));
            }
            return dtos;
        }

        public async Task<IEnumerable<ClaimResponseDto>> GetMyClaimsAsync(
            int customerId)
        {
            var claims = await _claimRepository.GetByCustomerIdAsync(customerId);
            var dtos = new List<ClaimResponseDto>();
            foreach (var c in claims)
            {
                dtos.Add(await MapToDtoAsync(c));
            }
            return dtos;
        }

        public async Task<IEnumerable<ClaimResponseDto>> GetMyAssignedClaimsAsync(
            int officerId)
        {
            var claims = await _claimRepository
                .GetByClaimsOfficerIdAsync(officerId);
            var dtos = new List<ClaimResponseDto>();
            foreach (var c in claims)
            {
                dtos.Add(await MapToDtoAsync(c));
            }
            return dtos;
        }

        public async Task<ClaimResponseDto> GetClaimByIdAsync(int id)
        {
            var claim = await _claimRepository.GetByIdWithDetailsAsync(id);
            if (claim == null)
                throw new NotFoundException("Claim", id);
            return await MapToDtoAsync(claim);
        }

        private async Task SaveClaimDocumentsAsync(
            List<IFormFile> files, int claimId, int uploadedByUserId)
        {
            if (files == null || files.Count == 0) return;

            var root = _environment?.WebRootPath ?? ".";
            var folderPath = root.Replace("\\", "/") + "/uploads/claims/" + claimId;

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            foreach (var file in files)
            {
                if (file == null) continue;
                var fileName = file.FileName ?? "unknown.doc";
                var ext = Path.GetExtension(fileName);
                var uniqueName = $"ClaimDoc_{claimId}_{Guid.NewGuid()}{ext}";
                var filePath = folderPath + "/" + uniqueName;

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

            // Notify Claims Officer that documents were uploaded
            var claim = await _claimRepository.GetByIdWithDetailsAsync(claimId);
            if (claim != null && claim.ClaimsOfficerId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: claim.ClaimsOfficerId.Value,
                    title: "New Claim Documents",
                    message: $"New documents have been uploaded for claim #{claimId} (Policy: {claim.PolicyAssignment?.PolicyNumber}).",
                    type: NotificationType.ClaimStatusUpdate,
                    policyId: claim.PolicyAssignmentId,
                    claimId: claimId,
                    paymentId: null);
            }
        }

        public async Task<(byte[] fileBytes, string fileName, string contentType)>
            DownloadClaimDocumentAsync(int claimId, int documentId, int userId, string role)
        {
            var claim = await _claimRepository.GetByIdWithDetailsAsync(claimId);
            if (claim == null)
                throw new NotFoundException("Claim", claimId);

            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null || document.ClaimId != claimId)
                throw new NotFoundException("Document", documentId);

            if (role == "Customer")
            {
                if (claim.PolicyAssignment?.CustomerId != userId)
                    throw new ForbiddenException("You can only download documents for your own claims");
            }
            else if (role == "ClaimsOfficer")
            {
                if (claim.ClaimsOfficerId != userId)
                    throw new ForbiddenException("You are not assigned to this claim");
            }

            var fullPath = Path.Combine(_environment.WebRootPath, document.FilePath);

            if (!File.Exists(fullPath))
                throw new NotFoundException("File not found on server", documentId);

            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var contentType = GetContentType(document.FileName);

            return (fileBytes, document.FileName, contentType);
        }

        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }

        private async Task<ClaimResponseDto> MapToDtoAsync(InsuranceClaim c)
        {
            var dto = new ClaimResponseDto
            {
                Id = c.Id,
                PolicyAssignmentId = c.PolicyAssignmentId,
                PolicyNumber = c.PolicyAssignment?.PolicyNumber ?? string.Empty,
                ClaimForMemberId = c.ClaimForMemberId > 0 ? c.ClaimForMemberId : c.PolicyMemberId,
                PolicyMemberName = c.PolicyMember?.MemberName ?? string.Empty,
                ClaimsOfficerId = c.ClaimsOfficerId,
                ClaimsOfficerName = c.ClaimsOfficer?.Name,
                ClaimType = c.ClaimType.ToString(),
                NomineeName = c.NomineeName,
                NomineeContact = c.NomineeContact,
                DeathCertificateNumber = c.DeathCertificateNumber,
                DateOfDeath = c.DateOfDeath,
                CauseOfDeath = c.CauseOfDeath,
                PlaceOfDeath = c.PlaceOfDeath,
                FiledDate = c.FiledDate,
                Status = c.Status.ToString(),
                Remarks = c.Remarks,
                OfficerRemarks = c.OfficerRemarks,
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

            if (c.PolicyAssignment != null)
            {
                dto.CustomerName = c.PolicyAssignment.Customer?.Name ?? string.Empty;
                dto.CustomerEmail = c.PolicyAssignment.Customer?.Email ?? string.Empty;
                dto.CustomerPhone = c.PolicyAssignment.Customer?.Phone ?? string.Empty;

                dto.PlanName = c.PolicyAssignment.Plan?.PlanName ?? string.Empty;
                dto.PolicyStatus = c.PolicyAssignment.Status.ToString();
                dto.PolicyStartDate = c.PolicyAssignment.StartDate;
                dto.PolicyTerm = c.PolicyAssignment.TermYears;
                
                dto.TotalPolicyCoverage = c.PolicyAssignment.PolicyMembers?.Sum(m => m.CoverageAmount) ?? 0;
                dto.RemainingCoverage = c.PolicyAssignment.PolicyMembers?
                    .Where(m => m.Status == MemberStatus.Active)
                    .Sum(m => m.CoverageAmount) ?? 0;

                // Payment Info
                var completedPayments = c.PolicyAssignment.Payments?
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList();
                
                dto.LastPaymentDate = completedPayments?.FirstOrDefault()?.PaymentDate;
                dto.TotalPaymentsCount = completedPayments?.Count ?? 0;
                dto.PremiumAmount = c.PolicyAssignment.TotalPremiumAmount;
                dto.NextDueDate = c.PolicyAssignment.NextDueDate;

                // Members
                dto.AllMembers = c.PolicyAssignment.PolicyMembers?.Select(m => new PolicyMemberResponseDto
                {
                    Id = m.Id,
                    MemberName = m.MemberName,
                    RelationshipToCustomer = m.RelationshipToCustomer,
                    DateOfBirth = m.DateOfBirth,
                    Gender = m.Gender,
                    CoverageAmount = m.CoverageAmount,
                    IsSmoker = m.IsSmoker,
                    HasPreExistingDiseases = m.HasPreExistingDiseases,
                    DiseaseDescription = m.DiseaseDescription,
                    Occupation = m.Occupation,
                    IsPrimaryInsured = m.IsPrimaryInsured,
                    Status = m.Status.ToString()
                }).ToList() ?? new();

                // Nominees
                dto.AllNominees = c.PolicyAssignment.PolicyNominees?.Select(n => new PolicyNomineeResponseDto
                {
                    Id = n.Id,
                    NomineeName = n.NomineeName,
                    RelationshipToPolicyHolder = n.RelationshipToPolicyHolder,
                    ContactNumber = n.ContactNumber,
                    SharePercentage = n.SharePercentage
                }).ToList() ?? new();
            }

            if (c.PolicyMember != null)
            {
                dto.MemberRelationship = c.PolicyMember.RelationshipToCustomer;
                dto.MemberDob = c.PolicyMember.DateOfBirth;
                dto.MemberStatus = c.PolicyMember.Status.ToString();
                
                // Age calculation
                dto.MemberAge = DateTime.Now.Year - c.PolicyMember.DateOfBirth.Year;
                if (c.PolicyMember.DateOfBirth.Date > DateTime.Now.AddYears(-dto.MemberAge)) dto.MemberAge--;
            }

            if (c.PolicyAssignment != null && c.PolicyMember != null)
            {
                try
                {
                    var currentCoverage = _policyService.GetCurrentCoverage(c.PolicyAssignment, c.PolicyMember);
                    var bonusResult = _policyService.GetBonusDetails(c.PolicyAssignment, c.PolicyMember);

                    dto.BaseCoverageAmount = currentCoverage;
                    dto.AccumulatedBonus = bonusResult.TotalBonus;
                    dto.TerminalBonus = bonusResult.TerminalBonus;

                    // Always recalculate view-only ClaimAmount for pending cases to reflect latest bonuses
                    if (c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview)
                    {
                        dto.ClaimAmount = Math.Round(currentCoverage + bonusResult.TotalBonus, 2);
                        if (c.ClaimType == ClaimType.Maturity)
                        {
                            dto.ClaimAmount = Math.Round(dto.ClaimAmount + bonusResult.TerminalBonus, 2);
                        }
                    }
                    else
                    {
                        dto.ClaimAmount = Math.Round(c.ClaimAmount, 2);
                    }
                }
                catch
                {
                    dto.ClaimAmount = c.ClaimAmount;
                }
            }
            else
            {
                dto.ClaimAmount = c.ClaimAmount;
            }

            // Loan Info
            var activeLoan = await _loanRepository.GetActiveLoanByPolicyAsync(c.PolicyAssignmentId);
            dto.OutstandingLoanAmount = Math.Round(activeLoan?.OutstandingBalance ?? 0, 2);
            dto.NetSettlementAmount = Math.Round(dto.ClaimAmount - dto.OutstandingLoanAmount, 2);

            // Calculate settlement breakdown if settled
            if (c.Status == ClaimStatus.Settled && (c.SettlementAmount ?? 0) > 0)
            {
                // Ensure nominees are loaded
                var policy = await _policyRepository.GetByIdWithDetailsAsync(c.PolicyAssignmentId);
                if (policy?.PolicyNominees != null)
                {
                    dto.SettlementBreakdown = policy.PolicyNominees.Select(n => new ClaimNomineeSettlementDto
                    {
                        NomineeName = n.NomineeName,
                        SharePercentage = n.SharePercentage,
                        SettlementAmount = Math.Round((c.SettlementAmount!.Value * n.SharePercentage) / 100, 2)
                    }).ToList();
                }
            }

            return dto;
        }
    }
}