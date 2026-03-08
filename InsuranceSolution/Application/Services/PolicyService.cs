// Application/Services/PolicyService.cs
using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class PolicyService : IPolicyService
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public PolicyService(
            IPolicyRepository policyRepository,
            IPlanRepository planRepository,
            IUserRepository userRepository,
            IDocumentRepository documentRepository,
            INotificationService notificationService,
            IEmailService emailService,
            IWebHostEnvironment environment)
        {
            _policyRepository = policyRepository;
            _planRepository = planRepository;
            _userRepository = userRepository;
            _documentRepository = documentRepository;
            _notificationService = notificationService;
            _emailService = emailService;
            _environment = environment;
        }

        public async Task<PolicyResponseDto> CreatePolicyAsync(
            int customerId,
            CreatePolicyDto dto,
            List<PolicyMemberDto> members,
            List<PolicyNomineeDto> nominees,
            List<IFormFile> customerDocuments,
            List<IFormFile> memberDocuments)
        {
            // Validate plan exists
            var plan = await _planRepository.GetByIdAsync(dto.PlanId);
            if (plan == null)
                throw new NotFoundException("Plan", dto.PlanId);

            if (!plan.IsActive)
                throw new BadRequestException("Selected plan is not active");

            if (dto.StartDate.Date <= DateTime.Today)
                throw new BadRequestException(
                    "Policy start date must be a future date");
            if (dto.StartDate.Date > DateTime.UtcNow.Date.AddYears(1))
                throw new BadRequestException(
                    "Policy start date cannot be more than 1 year in the future");

            // ── Validate TermYears against plan ──────────────────
            if (dto.TermYears < plan.MinTermYears || dto.TermYears > plan.MaxTermYears)
                throw new BadRequestException(
                    $"Term years must be between {plan.MinTermYears} " +
                    $"and {plan.MaxTermYears} years for this plan");

            // ── EndDate auto calculated from TermYears ────────────
            var endDate = dto.StartDate.AddYears(dto.TermYears);

            // Validate member count against plan limit
            if (members.Count > plan.MaxPolicyMembersAllowed)
                throw new BadRequestException(
                    $"This plan allows a maximum of " +
                    $"{plan.MaxPolicyMembersAllowed} members. " +
                    $"You provided {members.Count}.");

            // Validate exactly one primary insured
            var primaryCount = members.Count(m => m.IsPrimaryInsured);
            if (primaryCount != 1)
                throw new BadRequestException(
                    "Exactly one member must be marked as primary insured");

            // Validate member ages against plan limits
            foreach (var member in members)
            {
                var age = CalculateAge(member.DateOfBirth);
                if (age < plan.MinAge || age > plan.MaxAge)
                    throw new BadRequestException(
                        $"Member '{member.MemberName}' age {age} is outside " +
                        $"the plan's allowed age range " +
                        $"({plan.MinAge} - {plan.MaxAge})");
            }

            // Validate coverage amounts
            foreach (var member in members)
            {
                if (member.CoverageAmount < plan.MinCoverageAmount ||
                    member.CoverageAmount > plan.MaxCoverageAmount)
                    throw new BadRequestException(
                        $"Member '{member.MemberName}' coverage amount is outside " +
                        $"plan's allowed range " +
                        $"({plan.MinCoverageAmount} - {plan.MaxCoverageAmount})");
            }

            // Validate nominee share percentages total 100
            var totalShare = nominees.Sum(n => n.SharePercentage);
            if (totalShare != 100)
                throw new BadRequestException(
                    $"Nominee share percentages must total 100. Current total: {totalShare}");

            // ── Nominee count validation ──────────────────────────
            if (nominees.Count < plan.MinNominees)
                throw new BadRequestException(
                    $"This plan requires at least {plan.MinNominees} nominee(s). " +
                    $"You provided {nominees.Count}.");

            if (nominees.Count > plan.MaxNominees)
                throw new BadRequestException(
                    $"This plan allows a maximum of {plan.MaxNominees} nominee(s). " +
                    $"You provided {nominees.Count}.");

            var totalPremium = members.Sum(m =>
                CalculatePremium(
                    plan.BaseRate,
                    m.CoverageAmount,
                    dto.TermYears,
                    m.DateOfBirth,
                    m.IsSmoker,
                    m.Gender,
                    dto.PremiumFrequency
                ));
            var nextDueDate = dto.StartDate;

            // Build policy
            var policy = new PolicyAssignment
            {
                PolicyNumber = await _policyRepository.GeneratePolicyNumberAsync(),
                CustomerId = customerId,
                AgentId = null,         // default until admin assigns
                PlanId = dto.PlanId,
                StartDate = dto.StartDate,
                TermYears = dto.TermYears,
                EndDate = endDate,
                Status = PolicyStatus.Pending,
                TotalPremiumAmount = totalPremium,
                PremiumFrequency = dto.PremiumFrequency,
                NextDueDate = nextDueDate,
                CreatedAt = DateTime.UtcNow,
                PolicyMembers = members.Select(m => new PolicyMember
                {
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
                    CreatedAt = DateTime.UtcNow
                }).ToList(),
                PolicyNominees = nominees.Select(n => new PolicyNominee
                {
                    NomineeName = n.NomineeName,
                    RelationshipToPolicyHolder = n.RelationshipToPolicyHolder,
                    ContactNumber = n.ContactNumber,
                    SharePercentage = n.SharePercentage,
                    CreatedAt = DateTime.UtcNow
                }).ToList()
            };

            await _policyRepository.AddAsync(policy);
            await _policyRepository.SaveChangesAsync();

            await SaveCustomerDocumentsAsync(
                    customerDocuments, policy.Id, customerId);

            // Save member documents for non-primary members
            // Folder: uploads/policies/{policyId}/members/{memberId}/
            if (memberDocuments != null && memberDocuments.Any())
            {
                var nonPrimaryMembers = policy.PolicyMembers
                    .Where(m => !m.IsPrimaryInsured)
                    .ToList();

                await SaveMemberDocumentsAsync(
                    memberDocuments, policy.Id, nonPrimaryMembers, customerId);
            }

            var created = await _policyRepository.GetByIdWithDetailsAsync(policy.Id);
            return MapToDto(created!);
        }

        public async Task<PolicyResponseDto> GetPolicyByIdAsync(int id)
        {
            var policy = await _policyRepository.GetByIdWithDetailsAsync(id);
            if (policy == null)
                throw new NotFoundException("Policy", id);

            return MapToDto(policy);
        }

        public async Task<IEnumerable<PolicyResponseDto>> GetAllPoliciesAsync()
        {
            var policies = await _policyRepository.GetAllAsync();
            return policies.Select(MapToDto);
        }

        public async Task<IEnumerable<PolicyResponseDto>> GetMyPoliciesAsync(
            int customerId)
        {
            var policies = await _policyRepository
                .GetByCustomerIdAsync(customerId);
            return policies.Select(MapToDto);
        }

        public async Task<IEnumerable<PolicyResponseDto>> GetAgentPoliciesAsync(
            int agentId)
        {
            var policies = await _policyRepository.GetByAgentIdAsync(agentId);
            return policies.Select(MapToDto);
        }

        // Inject INotificationService and IEmailService and IUserRepository
        public async Task UpdatePolicyStatusAsync(int id, UpdatePolicyStatusDto dto)
        {
            var policy = await _policyRepository.GetByIdWithDetailsAsync(id);
            if (policy == null)
                throw new NotFoundException("Policy", id);

            policy.Status = dto.Status;
            if (dto.Status == PolicyStatus.Active)
                policy.AssignedDate = DateTime.UtcNow;

            _policyRepository.Update(policy);
            await _policyRepository.SaveChangesAsync();

            // Get customer details
            var customer = await _userRepository.GetByIdAsync(policy.CustomerId);

            // Send in-app notification
            await _notificationService.CreateNotificationAsync(
                userId: policy.CustomerId,
                title: "Policy Status Updated",
                message: $"Your policy {policy.PolicyNumber} status changed to {dto.Status}",
                type: NotificationType.PolicyStatusUpdate,
                policyId: policy.Id,
                claimId: null,
                paymentId: null);

            // Send email
            await _emailService.SendPolicyStatusChangedAsync(
                customer!.Email,
                customer.Name,
                policy.PolicyNumber,
                dto.Status.ToString());
        }

        public async Task AssignAgentAsync(int id, AssignAgentDto dto)
        {
            var policy = await _policyRepository.GetByIdAsync(id);
            if (policy == null)
                throw new NotFoundException("Policy", id);

            var agent = await _userRepository.GetByIdAsync(dto.AgentId);
            if (agent == null || agent.Role != UserRole.Agent)
                throw new BadRequestException(
                    "Provided user is not a valid agent");

            policy.AgentId = dto.AgentId;

            _policyRepository.Update(policy);
            await _policyRepository.SaveChangesAsync();
        }

        //  Private Helpers 

        // ── Customer Documents ────────────────────────────────
        private async Task SaveCustomerDocumentsAsync(
            List<IFormFile> files,
            int policyId,
            int uploadedByUserId)
        {
            // Folder structure: uploads/policies/{policyId}/customer/
            var folderPath = Path.Combine(
                _environment.WebRootPath,
                "uploads", "policies",
                policyId.ToString(),
                "customer");

            Directory.CreateDirectory(folderPath);

            var categories = new[] { "IdentityProof", "IncomeProof" };

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var category = i < categories.Length ? categories[i] : "CustomerDocument";

                // Filename: {category}_{policyId}_{guid}.{ext}
                var ext = Path.GetExtension(file.FileName);
                var uniqueName = $"{category}_{policyId}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(folderPath, uniqueName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                var document = new Document
                {
                    FileName = uniqueName,
                    FilePath = $"uploads/policies/{policyId}/customer/{uniqueName}",
                    DocumentCategory = category,
                    UploadedAt = DateTime.UtcNow,
                    UploadedByUserId = uploadedByUserId,
                    PolicyAssignmentId = policyId,
                    ClaimId = null
                };

                await _documentRepository.AddAsync(document);
            }

            await _documentRepository.SaveChangesAsync();
        }

        // ── Member Documents ──────────────────────────────────
        private async Task SaveMemberDocumentsAsync(
            List<IFormFile> files,
            int policyId,
            List<PolicyMember> nonPrimaryMembers,
            int uploadedByUserId)
        {
            // Distribute files evenly across non-primary members
            // Each non-primary member gets one identity proof document
            for (int i = 0; i < nonPrimaryMembers.Count && i < files.Count; i++)
            {
                var member = nonPrimaryMembers[i];
                var file = files[i];

                // Folder: uploads/policies/{policyId}/members/{memberId}/
                var folderPath = Path.Combine(
                    _environment.WebRootPath,
                    "uploads", "policies",
                    policyId.ToString(),
                    "members",
                    member.Id.ToString());

                Directory.CreateDirectory(folderPath);

                // Filename: IdentityProof_{memberId}_{guid}.{ext}
                var ext = Path.GetExtension(file.FileName);
                var uniqueName = $"IdentityProof_{member.Id}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(folderPath, uniqueName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                var document = new Document
                {
                    FileName = uniqueName,
                    FilePath = $"uploads/policies/{policyId}/members/{member.Id}/{uniqueName}",
                    DocumentCategory = "MemberIdentityProof",
                    UploadedAt = DateTime.UtcNow,
                    UploadedByUserId = uploadedByUserId,
                    PolicyAssignmentId = policyId,
                    ClaimId = null
                };

                await _documentRepository.AddAsync(document);
            }

            await _documentRepository.SaveChangesAsync();
        }

        private static decimal CalculatePremium(
            decimal baseRate,
            decimal coverageAmount,
            int termYears,
            DateTime dateOfBirth,
            bool isSmoker,
            string gender,
            PremiumFrequency frequency)
        {
            var annualPremium = (coverageAmount / 1000) * baseRate;

            var age = CalculateAge(dateOfBirth);  // reuse static age helper

            var ageFactor = age switch
            {
                <= 25 => 0.8m,
                <= 35 => 1.0m,
                <= 45 => 1.3m,
                <= 55 => 1.7m,
                _ => 2.2m
            };

            var smokerFactor = isSmoker ? 1.5m : 1.0m;
            var genderFactor = gender.ToLower() == "female" ? 0.9m : 1.0m;

            var termFactor = termYears switch
            {
                <= 10 => 1.0m,
                <= 20 => 1.1m,
                <= 30 => 1.2m,
                _ => 1.3m
            };

            var withGst = annualPremium * ageFactor * smokerFactor
                                        * genderFactor * termFactor * 1.18m;

            return frequency switch
            {
                PremiumFrequency.Monthly => Math.Round(withGst / 12, 2),
                PremiumFrequency.Quarterly => Math.Round(withGst / 4, 2),
                PremiumFrequency.Yearly => Math.Round(withGst, 2),
                _ => Math.Round(withGst, 2)
            };
        }

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
        public async Task<(byte[] fileBytes, string fileName, string contentType)>
    DownloadDocumentAsync(int documentId, int userId, string role)
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
                throw new NotFoundException("Document", documentId);

            // Customers can only download their own documents
            if (role == "Customer")
            {
                var policy = await _policyRepository
                    .GetByIdAsync(document.PolicyAssignmentId!.Value);

                if (policy == null || policy.CustomerId != userId)
                    throw new ForbiddenException(
                        "You can only download your own documents");
            }

            var fullPath = Path.Combine(
                _environment.WebRootPath, document.FilePath);

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

        private static PolicyResponseDto MapToDto(PolicyAssignment p) => new()
        {
            Id = p.Id,
            PolicyNumber = p.PolicyNumber,
            CustomerId = p.CustomerId,
            CustomerName = p.Customer?.Name ?? string.Empty,
            AgentId = p.AgentId,
            AgentName = p.Agent?.Name,
            PlanId = p.PlanId,
            PlanName = p.Plan?.PlanName ?? string.Empty,
            StartDate = p.StartDate,
            TermYears = p.TermYears,
            EndDate = p.EndDate,
            Status = p.Status.ToString(),
            CommissionStatus = p.CommissionStatus.ToString(),
            TotalPremiumAmount = p.TotalPremiumAmount,
            PremiumFrequency = p.PremiumFrequency.ToString(),
            NextDueDate = p.NextDueDate,
            CreatedAt = p.CreatedAt,
            Members = p.PolicyMembers?.Select(m => new PolicyMemberResponseDto
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
                IsPrimaryInsured = m.IsPrimaryInsured
            }).ToList() ?? new(),
            Nominees = p.PolicyNominees?.Select(n => new PolicyNomineeResponseDto
            {
                Id = n.Id,
                NomineeName = n.NomineeName,
                RelationshipToPolicyHolder = n.RelationshipToPolicyHolder,
                ContactNumber = n.ContactNumber,
                SharePercentage = n.SharePercentage
            }).ToList() ?? new(),
            Documents = p.Documents?.Select(d => new DocumentResponseDto
            {
                Id = d.Id,
                FileName = d.FileName,
                FilePath = d.FilePath,
                DocumentCategory = d.DocumentCategory,
                UploadedAt = d.UploadedAt
            }).ToList() ?? new()
        };
        public async Task CancelPendingPolicyAsync(int policyId, int customerId)
        {
            var policy = await _policyRepository.GetByIdAsync(policyId);

            if (policy == null)
                throw new NotFoundException("Policy", policyId);

            if (policy.CustomerId != customerId)
                throw new ForbiddenException("You can only cancel your own policies");

            // Strict validation: Only Pending policies can be cancelled by the user
            if (policy.Status != PolicyStatus.Pending)
                throw new BadRequestException($"Cannot cancel policy with status: {policy.Status}");

            policy.Status = PolicyStatus.Cancelled; // Or create a 'Cancelled' status in your Enum

            _policyRepository.Update(policy);
            await _policyRepository.SaveChangesAsync();
        }
        public async Task<PolicyResponseDto> SaveDraftAsync(
    int customerId, SaveDraftDto dto)
        {
            // Validate plan if provided
            Plan? plan = null;
            if (dto.PlanId.HasValue)
            {
                plan = await _planRepository.GetByIdAsync(dto.PlanId.Value);
                if (plan == null)
                    throw new NotFoundException("Plan", dto.PlanId.Value);

                if (!plan.IsActive)
                    throw new BadRequestException("Selected plan is not active");
            }

            // Build draft policy — all validations are skipped
            var draft = new PolicyAssignment
            {
                PolicyNumber = await _policyRepository
                                      .GeneratePolicyNumberAsync(),
                CustomerId = customerId,
                AgentId = null,
                PlanId = dto.PlanId ?? 0,
                StartDate = dto.StartDate ?? DateTime.UtcNow.AddDays(1),
                TermYears = dto.TermYears ?? 0,
                EndDate = dto.StartDate.HasValue && dto.TermYears.HasValue
                                      ? dto.StartDate.Value
                                            .AddYears(dto.TermYears.Value)
                                      : DateTime.UtcNow.AddYears(1),
                Status = PolicyStatus.Draft,
                TotalPremiumAmount = 0,          // calculated on submit
                PremiumFrequency = dto.PremiumFrequency
                                         ?? PremiumFrequency.Monthly,
                NextDueDate = dto.StartDate ?? DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow,

                // Save partial members if provided
                PolicyMembers = dto.Members?.Select(m => new PolicyMember
                {
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
                    CreatedAt = DateTime.UtcNow
                }).ToList() ?? new(),

                // Save partial nominees if provided
                PolicyNominees = dto.Nominees?.Select(n => new PolicyNominee
                {
                    NomineeName = n.NomineeName,
                    RelationshipToPolicyHolder = n.RelationshipToPolicyHolder,
                    ContactNumber = n.ContactNumber,
                    SharePercentage = n.SharePercentage,
                    CreatedAt = DateTime.UtcNow
                }).ToList() ?? new()
            };

            await _policyRepository.AddAsync(draft);
            await _policyRepository.SaveChangesAsync();

            var created = await _policyRepository
                .GetByIdWithDetailsAsync(draft.Id);
            return MapToDto(created!);
        }

        public async Task<PolicyResponseDto> UpdateDraftAsync(
            int policyId, int customerId, SaveDraftDto dto)
        {
            var draft = await _policyRepository
                .GetByIdWithDetailsAsync(policyId);

            if (draft == null)
                throw new NotFoundException("Draft", policyId);

            if (draft.CustomerId != customerId)
                throw new ForbiddenException(
                    "You can only update your own drafts");

            if (draft.Status != PolicyStatus.Draft)
                throw new BadRequestException(
                    "Only draft policies can be updated this way");

            // Update fields if provided
            if (dto.PlanId.HasValue)
            {
                var plan = await _planRepository.GetByIdAsync(dto.PlanId.Value);
                if (plan == null)
                    throw new NotFoundException("Plan", dto.PlanId.Value);
                draft.PlanId = dto.PlanId.Value;
            }

            if (dto.StartDate.HasValue)
            {
                draft.StartDate = dto.StartDate.Value;
                draft.NextDueDate = dto.StartDate.Value;
            }

            if (dto.TermYears.HasValue)
                draft.TermYears = dto.TermYears.Value;

            if (dto.PremiumFrequency.HasValue)
                draft.PremiumFrequency = dto.PremiumFrequency.Value;

            // Recalculate EndDate if both StartDate and TermYears available
            if (draft.StartDate != default && draft.TermYears > 0)
                draft.EndDate = draft.StartDate.AddYears(draft.TermYears);

            // Update members — replace all
            if (dto.Members != null)
            {
                draft.PolicyMembers = dto.Members.Select(m => new PolicyMember
                {
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
                    CreatedAt = DateTime.UtcNow
                }).ToList();
            }

            // Update nominees — replace all
            if (dto.Nominees != null)
            {
                draft.PolicyNominees = dto.Nominees.Select(n => new PolicyNominee
                {
                    NomineeName = n.NomineeName,
                    RelationshipToPolicyHolder = n.RelationshipToPolicyHolder,
                    ContactNumber = n.ContactNumber,
                    SharePercentage = n.SharePercentage,
                    CreatedAt = DateTime.UtcNow
                }).ToList();
            }

            _policyRepository.Update(draft);
            await _policyRepository.SaveChangesAsync();

            var updated = await _policyRepository
                .GetByIdWithDetailsAsync(draft.Id);
            return MapToDto(updated!);
        }

        public async Task<PolicyResponseDto> SubmitDraftAsync(
            int policyId,
            int customerId,
            CreatePolicyDto dto,
            List<PolicyMemberDto> members,
            List<PolicyNomineeDto> nominees,
            List<IFormFile> customerDocuments,
            List<IFormFile> memberDocuments)
        {
            var draft = await _policyRepository
                .GetByIdWithDetailsAsync(policyId);

            if (draft == null)
                throw new NotFoundException("Draft", policyId);

            if (draft.CustomerId != customerId)
                throw new ForbiddenException(
                    "You can only submit your own drafts");

            if (draft.Status != PolicyStatus.Draft)
                throw new BadRequestException(
                    "Only draft policies can be submitted");

            // Run all full validations same as CreatePolicyAsync
            var plan = await _planRepository.GetByIdAsync(dto.PlanId);
            if (plan == null)
                throw new NotFoundException("Plan", dto.PlanId);

            if (!plan.IsActive)
                throw new BadRequestException("Selected plan is not active");

            if (dto.StartDate.Date <= DateTime.Today)
                throw new BadRequestException(
                    "Policy start date must be a future date");

            if (dto.StartDate.Date > DateTime.UtcNow.Date.AddYears(1))
                throw new BadRequestException(
                    "Start date cannot be more than 1 year in the future");

            if (dto.TermYears < plan.MinTermYears ||
                dto.TermYears > plan.MaxTermYears)
                throw new BadRequestException(
                    $"Term years must be between {plan.MinTermYears} " +
                    $"and {plan.MaxTermYears}");

            if (members.Count > plan.MaxPolicyMembersAllowed)
                throw new BadRequestException(
                    $"Max {plan.MaxPolicyMembersAllowed} members allowed");

            var primaryCount = members.Count(m => m.IsPrimaryInsured);
            if (primaryCount != 1)
                throw new BadRequestException(
                    "Exactly one primary insured required");

            foreach (var member in members)
            {
                var age = CalculateAge(member.DateOfBirth);
                if (age < plan.MinAge || age > plan.MaxAge)
                    throw new BadRequestException(
                        $"Member '{member.MemberName}' age {age} is outside " +
                        $"plan range ({plan.MinAge}-{plan.MaxAge})");

                if (member.CoverageAmount < plan.MinCoverageAmount ||
                    member.CoverageAmount > plan.MaxCoverageAmount)
                    throw new BadRequestException(
                        $"Member '{member.MemberName}' coverage outside " +
                        $"plan range");
            }

            var totalShare = nominees.Sum(n => n.SharePercentage);
            if (totalShare != 100)
                throw new BadRequestException(
                    $"Nominee shares must total 100. Current: {totalShare}");

            if (nominees.Count < plan.MinNominees)
                throw new BadRequestException(
                    $"Minimum {plan.MinNominees} nominee(s) required");

            if (nominees.Count > plan.MaxNominees)
                throw new BadRequestException(
                    $"Maximum {plan.MaxNominees} nominee(s) allowed");

            // Calculate premium
            var totalPremium = members.Sum(m =>
                CalculatePremium(
                    plan.BaseRate,
                    m.CoverageAmount,
                    dto.TermYears,
                    m.DateOfBirth,
                    m.IsSmoker,
                    m.Gender,
                    dto.PremiumFrequency));

            // Update draft → Pending
            draft.PlanId = dto.PlanId;
            draft.StartDate = dto.StartDate;
            draft.TermYears = dto.TermYears;
            draft.EndDate = dto.StartDate.AddYears(dto.TermYears);
            draft.PremiumFrequency = dto.PremiumFrequency;
            draft.TotalPremiumAmount = totalPremium;
            draft.NextDueDate = dto.StartDate;
            draft.Status = PolicyStatus.Pending;
            draft.AgentId = null;

            // Replace members and nominees
            draft.PolicyMembers = members.Select(m => new PolicyMember
            {
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
                CreatedAt = DateTime.UtcNow
            }).ToList();

            draft.PolicyNominees = nominees.Select(n => new PolicyNominee
            {
                NomineeName = n.NomineeName,
                RelationshipToPolicyHolder = n.RelationshipToPolicyHolder,
                ContactNumber = n.ContactNumber,
                SharePercentage = n.SharePercentage,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _policyRepository.Update(draft);
            await _policyRepository.SaveChangesAsync();

            // Save documents
            await SaveCustomerDocumentsAsync(
                customerDocuments, draft.Id, customerId);

            if (memberDocuments != null && memberDocuments.Any())
            {
                var nonPrimaryMembers = draft.PolicyMembers
                    .Where(m => !m.IsPrimaryInsured).ToList();
                await SaveMemberDocumentsAsync(
                    memberDocuments, draft.Id, nonPrimaryMembers, customerId);
            }

            // Notify customer
            await _notificationService.CreateNotificationAsync(
                userId: customerId,
                title: "Policy Submitted",
                message: $"Your policy {draft.PolicyNumber} has been " +
                          $"submitted for review.",
                type: NotificationType.PolicyStatusUpdate,
                policyId: draft.Id,
                claimId: null,
                paymentId: null);

            var updated = await _policyRepository
                .GetByIdWithDetailsAsync(draft.Id);
            return MapToDto(updated!);
        }

        public async Task<IEnumerable<PolicyResponseDto>> GetMyDraftsAsync(
            int customerId)
        {
            var policies = await _policyRepository
                .GetByCustomerIdAsync(customerId);

            return policies
                .Where(p => p.Status == PolicyStatus.Draft)
                .Select(MapToDto);
        }

        public async Task DeleteDraftAsync(int policyId, int customerId)
        {
            var draft = await _policyRepository.GetByIdAsync(policyId);

            if (draft == null)
                throw new NotFoundException("Draft", policyId);

            if (draft.CustomerId != customerId)
                throw new ForbiddenException(
                    "You can only delete your own drafts");

            if (draft.Status != PolicyStatus.Draft)
                throw new BadRequestException(
                    "Only draft policies can be deleted");

            _policyRepository.Delete(draft);
            await _policyRepository.SaveChangesAsync();
        }
    }
}
