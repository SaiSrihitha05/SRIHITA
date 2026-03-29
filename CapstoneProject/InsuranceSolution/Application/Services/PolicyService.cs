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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Unit = QuestPDF.Infrastructure.Unit;

namespace Application.Services
{
    public class PolicyService : IPolicyService
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly IEmailTemplateService _templateService;
        private readonly IPdfService _pdfService;
        private readonly IChatMessageRepository _chatRepo;
        public PolicyService(
            IPolicyRepository policyRepository,
            IPlanRepository planRepository,
            IUserRepository userRepository,
            IDocumentRepository documentRepository,
            INotificationService notificationService,
            IEmailService emailService,
            IEmailTemplateService templateService,
            IPdfService pdfService,
            IWebHostEnvironment environment,
            ISystemConfigRepository systemConfigRepository,
            IChatMessageRepository chatRepo)
        {
            _policyRepository = policyRepository;
            _planRepository = planRepository;
            _userRepository = userRepository;
            _documentRepository = documentRepository;
            _notificationService = notificationService;
            _emailService = emailService;
            _templateService = templateService;
            _pdfService = pdfService;
            _environment = environment;
            _systemConfigRepository = systemConfigRepository;
            _chatRepo = chatRepo;
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

            var customer = await _userRepository.GetByIdAsync(customerId);
            if (customer == null) throw new NotFoundException("Customer", customerId);

            // Enforce Profile Consistency for "Self" Relationship
            foreach (var member in members)
            {
                var isSelf = member.RelationshipToCustomer.Equals("Self", StringComparison.OrdinalIgnoreCase);
                if (isSelf)
                {
                    if (!member.MemberName.Equals(customer.Name, StringComparison.OrdinalIgnoreCase))
                        throw new BadRequestException($"Member marked as 'Self' must match customer name '{customer.Name}'");

                    if (customer.DateOfBirth.HasValue && member.DateOfBirth?.Date != customer.DateOfBirth.Value.Date)
                        throw new BadRequestException($"Member marked as 'Self' must match customer DOB '{customer.DateOfBirth:yyyy-MM-dd}'");

                    if (!string.IsNullOrEmpty(customer.Gender) && !member.Gender.Equals(customer.Gender, StringComparison.OrdinalIgnoreCase))
                        throw new BadRequestException($"Member marked as 'Self' must match customer gender '{customer.Gender}'");
                }
                else
                {
                    // If relationship is NOT self, ensure name is NOT exactly matching customer name 
                    // to prevent accidental duplicate entries of the same person under wrong relation.
                    if (member.MemberName.Equals(customer.Name, StringComparison.OrdinalIgnoreCase))
                        throw new BadRequestException($"Member '{member.MemberName}' cannot have relationship '{member.RelationshipToCustomer}' if name matches customer. Did you mean 'Self'?");
                }
            }

            if (dto.StartDate.Date < DateTime.Today)
                throw new BadRequestException(
                    "Policy start date cannot be in the past");
            if (dto.StartDate.Date > DateTime.UtcNow.Date.AddYears(1))
                throw new BadRequestException(
                    "Policy start date cannot be more than 1 year in the future");

            // Validate TermYears against plan (Skip if lifelong coverage)
            if (!plan.IsCoverageUntilAge)
            {
                if (dto.TermYears < plan.MinTermYears || dto.TermYears > plan.MaxTermYears)
                    throw new BadRequestException(
                        $"Term years must be between {plan.MinTermYears} " +
                        $"and {plan.MaxTermYears} years for this plan");
            }

            // EndDate auto calculated from TermYears 
            if (plan.IsCoverageUntilAge)
            {
                var primaryMember = members.FirstOrDefault(m => m.IsPrimaryInsured);
                if (primaryMember == null) throw new BadRequestException("Primary insured is missing");

                var entryAge = CalculateAge(primaryMember.DateOfBirth ?? DateTime.Today);
                dto.TermYears = (plan.CoverageUntilAge ?? 100) - entryAge;
            }

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
                var age = CalculateAge(member.DateOfBirth ?? DateTime.Today);
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

            // Nominee count validation 
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
                    m.DateOfBirth ?? DateTime.Today,
                    m.IsSmoker,
                    m.Gender ?? string.Empty,
                    dto.PremiumFrequency
                ));
            var nextDueDate = dto.StartDate;

            // Build policy
            var policy = new PolicyAssignment
            {
                PolicyNumber = await _policyRepository.GeneratePolicyNumberAsync(),
                CustomerId = customerId,
                AgentId = null,
                PlanId = dto.PlanId,
                StartDate = dto.StartDate,
                TermYears = dto.TermYears,
                EndDate = endDate,
                Status = PolicyStatus.Pending,
                TotalPremiumAmount = totalPremium,
                PremiumFrequency = dto.PremiumFrequency,
                Address = dto.Address,
                NextDueDate = nextDueDate,
                CreatedAt = DateTime.UtcNow,
                PolicyMembers = members.Select(m => new PolicyMember
                {
                    MemberName = m.MemberName,
                    RelationshipToCustomer = m.RelationshipToCustomer,
                    DateOfBirth = m.DateOfBirth ?? DateTime.MinValue,
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

            // Round Robin assignment for Agent at submission time
            var submissionAgents = (await _userRepository.GetByRoleAsync(UserRole.Agent))
                                    .Where(u => u.IsActive)
                                    .OrderBy(u => u.Id)
                                    .ToList();
            Console.WriteLine($"[PolicyService][CreatePolicy] Active agents found: {submissionAgents.Count}");
            if (submissionAgents.Any())
            {
                // Assign by Chat Interaction History if available
                var chatHistory = await _chatRepo.GetBySessionAsync(customerId, null);
                var lastChatAgentId = chatHistory.LastOrDefault(m => m.AgentId != null)?.AgentId;

                if (lastChatAgentId.HasValue && submissionAgents.Any(a => a.Id == lastChatAgentId.Value))
                {
                    policy.AgentId = lastChatAgentId.Value;
                    Console.WriteLine($"[PolicyService][CreatePolicy] Assigned AgentId={policy.AgentId} BASED ON CHAT HISTORY.");
                }
                else
                {
                    var config = await _systemConfigRepository.GetConfigAsync();
                    int nextIndex = (config.LastAgentAssignmentIndex + 1) % submissionAgents.Count;
                    policy.AgentId = submissionAgents[nextIndex].Id;
                    config.LastAgentAssignmentIndex = nextIndex;
                    _systemConfigRepository.Update(config);
                    await _systemConfigRepository.SaveChangesAsync();
                    Console.WriteLine($"[PolicyService][CreatePolicy] Assigned AgentId={policy.AgentId} (index={nextIndex}) via Round Robin.");
                }
            }
            else
            {
                Console.WriteLine("[PolicyService][CreatePolicy] WARNING: No active agents found – policy will be Unassigned.");
            }

            await _policyRepository.AddAsync(policy);
            await _policyRepository.SaveChangesAsync();

            // Notify assigned agent (if any) — after policy is saved and has an ID
            if (policy.AgentId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: policy.AgentId.Value,
                    title: "New Policy Assigned",
                    message: $"A new policy application {policy.PolicyNumber} has been assigned to you for review.",
                    type: NotificationType.General,
                    policyId: policy.Id,
                    claimId: null,
                    paymentId: null);
            }

            await SaveCustomerDocumentsAsync(
                    customerDocuments, policy.Id, customerId);

            // Save member documents for non-primary members
            if (memberDocuments != null && memberDocuments.Any())
            {
                var nonPrimaryMembers = policy.PolicyMembers
                    .Where(m => !m.IsPrimaryInsured)
                    .ToList();

                await SaveMemberDocumentsAsync(
                    memberDocuments, policy.Id, nonPrimaryMembers, customerId);
            }

            var created = await _policyRepository.GetByIdWithDetailsAsync(policy.Id);

            // Notify Customer
            await _notificationService.CreateNotificationAsync(
                userId: customerId,
                title: "Policy Submitted",
                message: $"Your application for {created!.Plan?.PlanName} has been submitted for review. Policy Number: {created.PolicyNumber}",
                type: NotificationType.General,
                policyId: created.Id,
                claimId: null,
                paymentId: null);

            // Notify Admin
            var admins = await _userRepository.GetByRoleAsync(UserRole.Admin);
            foreach (var admin in admins)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: admin.Id,
                    title: "New Policy Submitted",
                    message: $"A new policy application {created.PolicyNumber} has been submitted by {created.Customer?.Name}.",
                    type: NotificationType.General,
                    policyId: created.Id,
                    claimId: null,
                    paymentId: null);
            }

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
            return policies
                .OrderByDescending(p => p.CreatedAt)
                .Select(MapToDto);
        }

        public async Task<IEnumerable<PolicyResponseDto>> GetMyPoliciesAsync(
            int customerId)
        {
            var policies = await _policyRepository
                .GetByCustomerIdAsync(customerId);
            return policies
                .OrderByDescending(p => p.CreatedAt)
                .Select(MapToDto);
        }

        public async Task<IEnumerable<PolicyResponseDto>> GetAgentPoliciesAsync(
            int agentId)
        {
            var policies = await _policyRepository.GetByAgentIdAsync(agentId);
            return policies
                .OrderByDescending(p => p.CreatedAt)
                .Select(MapToDto);
        }

        public async Task UpdatePolicyStatusAsync(int id, UpdatePolicyStatusDto dto)
        {
            var policy = await _policyRepository.GetByIdWithDetailsAsync(id);
            if (policy == null)
                throw new NotFoundException("Policy", id);

            policy.Status = dto.Status;
            policy.Remarks = dto.Remarks;
            if (dto.Status == PolicyStatus.Active)
            {
                policy.AssignedDate = DateTime.UtcNow;

                // Round Robin assignment for Agent if not already assigned
                if (policy.AgentId == null)
                {
                    var agents = (await _userRepository.GetByRoleAsync(UserRole.Agent))
                                    .Where(u => u.IsActive)
                                    .OrderBy(u => u.Id)
                                    .ToList();

                    if (agents.Any())
                    {
                        var config = await _systemConfigRepository.GetConfigAsync();
                        int nextIndex = (config.LastAgentAssignmentIndex + 1) % agents.Count;
                        policy.AgentId = agents[nextIndex].Id;

                        config.LastAgentAssignmentIndex = nextIndex;
                        _systemConfigRepository.Update(config);
                        await _systemConfigRepository.SaveChangesAsync();
                    }
                }
            }

            _policyRepository.Update(policy);
            await _policyRepository.SaveChangesAsync();

            // Get customer details
            var customer = await _userRepository.GetByIdAsync(policy.CustomerId);

            try
            {
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
                string emailBody;
                var emailRequest = new EmailRequest
                {
                    ToEmail = customer!.Email,
                    ToName = customer.Name,
                    Subject = $"Policy Status Update - {policy.PolicyNumber}"
                };

                if (dto.Status == PolicyStatus.Active)
                {
                    emailBody = _templateService.GetPolicyApprovedTemplate(customer.Name, policy.PolicyNumber);
                    var policyDto = MapToDto(policy);
                    var pdfBytes = await _pdfService.GeneratePolicyPdfAsync(policyDto);
                    
                    emailRequest.Attachments = new List<EmailAttachment>
                    {
                        new EmailAttachment { Name = $"Policy_{policy.PolicyNumber}.pdf", Content = pdfBytes }
                    };
                }
                else if (dto.Status == PolicyStatus.Rejected)
                {
                    emailBody = _templateService.GetPolicyRejectedTemplate(customer.Name, policy.Remarks ?? "Documents incomplete or invalid.");
                }
                else
                {
                    emailBody = _templateService.GetGenericNotificationTemplate("Policy Status Updated", 
                        $"Your policy {policy.PolicyNumber} status has been updated to {dto.Status}.");
                }

                emailRequest.HtmlContent = emailBody;
                await _emailService.SendEmailAsync(emailRequest);

                // Notify Agent if assigned
                if (policy.AgentId.HasValue)
                {
                    await _notificationService.CreateNotificationAsync(
                        userId: policy.AgentId.Value,
                        title: "Policy Status Updated",
                        message: $"Policy {policy.PolicyNumber} for {customer.Name} has been {dto.Status}.",
                        type: NotificationType.PolicyStatusUpdate,
                        policyId: policy.Id,
                        claimId: null,
                        paymentId: null);
                }
            }
            catch (Exception ex)
            {
                // Simply swallow the exception so the HTTP request still succeeds.
                // In a production system, use ILogger to log this failure here.
                Console.WriteLine($"Failed to send post-update notifications for policy {policy.Id}: {ex.Message}");
            }
        }

        public async Task AssignAgentAsync(int id, AssignAgentDto dto)
        {
            var policy = await _policyRepository.GetByIdAsync(id);
            if (policy == null)
                throw new NotFoundException("Policy", id);

            if (policy.Status == Domain.Enums.PolicyStatus.Cancelled)
                throw new BadRequestException("Cannot assign an agent to a cancelled policy.");

            var agent = await _userRepository.GetByIdAsync(dto.AgentId);
            if (agent == null || agent.Role != UserRole.Agent)
                throw new BadRequestException(
                    "Provided user is not a valid agent");

            policy.AgentId = dto.AgentId;

            _policyRepository.Update(policy);
            await _policyRepository.SaveChangesAsync();

            // Notify customer
            await _notificationService.CreateNotificationAsync(
                userId: policy.CustomerId,
                title: "Agent Assigned",
                message: $"Agent {agent.Name} (ID: {agent.Id}) has been assigned to your policy {policy.PolicyNumber}. They will assist you with any queries.",
                type: NotificationType.General,
                policyId: policy.Id,
                claimId: null,
                paymentId: null);

            // Notify agent
            await _notificationService.CreateNotificationAsync(
                userId: dto.AgentId,
                title: "New Policy Assigned",
                message: $"You have been assigned to manage policy {policy.PolicyNumber}.",
                type: NotificationType.General,
                policyId: policy.Id,
                claimId: null,
                paymentId: null);

            // Notify Admin
            var admins = await _userRepository.GetByRoleAsync(UserRole.Admin);
            foreach (var admin in admins)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: admin.Id,
                    title: "Agent Assigned to Policy",
                    message: $"Agent {agent.Name} has been assigned to policy {policy.PolicyNumber}.",
                    type: NotificationType.General,
                    policyId: policy.Id,
                    claimId: null,
                    paymentId: null);
            }
        }

        // Customer Documents 
        private async Task SaveCustomerDocumentsAsync(
            List<IFormFile> files,
            int policyId,
            int uploadedByUserId)
        {
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

                var document = new Domain.Entities.Document
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

        // Member Documents
        private async Task SaveMemberDocumentsAsync(
            List<IFormFile> files,
            int policyId,
            List<PolicyMember> nonPrimaryMembers,
            int uploadedByUserId)
        {
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

                var document = new Domain.Entities.Document
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

            var age = CalculateAge(dateOfBirth);

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

        private PolicyResponseDto MapToDto(PolicyAssignment p)
        {
            var dto = new PolicyResponseDto
            {
                Id = p.Id,
                PolicyNumber = p.PolicyNumber,
                CustomerId = p.CustomerId,
                CustomerName = p.Customer?.Name ?? string.Empty,
                AgentId = p.AgentId,
                AgentName = p.Agent?.Name,
                AgentEmail = p.Agent?.Email,
                AgentPhone = p.Agent?.Phone,
                PlanId = p.PlanId,
                PlanName = p.Plan?.PlanName ?? string.Empty,
                StartDate = p.StartDate,
                TermYears = p.TermYears,
                EndDate = p.EndDate,
                Status = p.Status.ToString(),
                Remarks = p.Remarks,
                CommissionStatus = p.CommissionStatus.ToString(),
                TotalPremiumAmount = p.TotalPremiumAmount,
                PremiumFrequency = p.PremiumFrequency.ToString(),
                NextDueDate = p.NextDueDate,
                Address = p.Address,
                CanRenew = Math.Abs((p.EndDate - DateTime.Today).TotalDays) <= 30 && (p.Status == PolicyStatus.Active || p.Status == PolicyStatus.Expired),
                CreatedAt = p.CreatedAt,
                CustomerPhone = p.Customer?.Phone ?? string.Empty,
                CustomerEmail = p.Customer?.Email ?? string.Empty,
                CustomerAge = p.PolicyMembers?.FirstOrDefault(m => m.IsPrimaryInsured)?.DateOfBirth != null
                    ? CalculateAge(p.PolicyMembers.First(m => m.IsPrimaryInsured).DateOfBirth)
                    : 0,
                PlanHasLoanFacility = p.Plan?.HasLoanFacility ?? false,
                PlanLoanEligibleAfterYears = p.Plan?.LoanEligibleAfterYears ?? 0,
                PlanMaxLoanPercentage = p.Plan?.MaxLoanPercentage ?? 0,
                PlanCoverageIncreasing = p.Plan?.CoverageIncreasing ?? false,
                PlanCoverageIncreaseRate = p.Plan?.CoverageIncreaseRate ?? 0,
                PlanHasBonus = p.Plan?.HasBonus ?? false,
                PlanBonusRate = p.Plan?.BonusRate ?? 0,
                PlanTerminalBonusRate = p.Plan?.TerminalBonusRate ?? 0,
                PlanIsCoverageUntilAge = p.Plan?.IsCoverageUntilAge ?? false,
                PlanCoverageUntilAge = p.Plan?.CoverageUntilAge ?? 0,
                PlanMinAge = p.Plan?.MinAge ?? 0,
                PlanMaxAge = p.Plan?.MaxAge ?? 0,
                PlanMinCoverageAmount = p.Plan?.MinCoverageAmount ?? 0,
                PlanMaxCoverageAmount = p.Plan?.MaxCoverageAmount ?? 0,
                PlanMinNominees = p.Plan?.MinNominees ?? 0,
                PlanMaxNominees = p.Plan?.MaxNominees ?? 0,
                PlanMaxMembers = p.Plan?.MaxPolicyMembersAllowed ?? 0,
                HasPaidPremiums = p.Payments?.Any(pay => pay.Status == PaymentStatus.Completed),
                PlanGracePeriodDays = p.Plan?.GracePeriodDays,
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
                    UploadedByName = d.UploadedByUser?.Name ?? "Customer",
                    UploadedAt = d.UploadedAt,
                    Status = d.Status.ToString()
                }).ToList() ?? new()
            };

            dto.Members = p.PolicyMembers?.Select(m => new PolicyMemberResponseDto
            {
                Id = m.Id,
                MemberName = m.MemberName,
                RelationshipToCustomer = m.RelationshipToCustomer,
                DateOfBirth = m.DateOfBirth,
                Gender = m.Gender,
                CoverageAmount = m.CoverageAmount,
                CurrentCoverageAmount = GetCurrentCoverage(p, m),
                YearsActive = (DateTime.UtcNow - p.StartDate).Days / 365,
                IsSmoker = m.IsSmoker,
                HasPreExistingDiseases = m.HasPreExistingDiseases,
                DiseaseDescription = m.DiseaseDescription,
                Occupation = m.Occupation,
                IsPrimaryInsured = m.IsPrimaryInsured,
                Status = m.Status.ToString()
            }).ToList() ?? new();

            var primaryMember = p.PolicyMembers?.FirstOrDefault(m => m.IsPrimaryInsured);
            if (primaryMember != null)
            {
                dto.BonusDetails = GetBonusDetails(p, primaryMember);
            }

            return dto;
        }
        public async Task CancelPendingPolicyAsync(int policyId, int customerId)
        {
            var policy = await _policyRepository.GetByIdAsync(policyId);

            if (policy == null)
                throw new NotFoundException("Policy", policyId);

            if (policy.CustomerId != customerId)
                throw new ForbiddenException("You can only cancel your own policies");

            // Only Pending policies can be cancelled by the user
            if (policy.Status != PolicyStatus.Pending)
                throw new BadRequestException($"Cannot cancel policy with status: {policy.Status}");

            policy.Status = PolicyStatus.Cancelled;

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
                    MemberName = m.MemberName ?? string.Empty,
                    RelationshipToCustomer = m.RelationshipToCustomer ?? string.Empty,
                    DateOfBirth = m.DateOfBirth ?? DateTime.MinValue,
                    Gender = m.Gender ?? string.Empty,
                    CoverageAmount = m.CoverageAmount,
                    IsSmoker = m.IsSmoker,
                    HasPreExistingDiseases = m.HasPreExistingDiseases,
                    DiseaseDescription = m.DiseaseDescription,
                    Occupation = m.Occupation ?? string.Empty,
                    IsPrimaryInsured = m.IsPrimaryInsured,
                    CreatedAt = DateTime.UtcNow
                }).ToList() ?? new(),

                // Save partial nominees if provided
                PolicyNominees = dto.Nominees?.Select(n => new PolicyNominee
                {
                    NomineeName = n.NomineeName ?? string.Empty,
                    RelationshipToPolicyHolder = n.RelationshipToPolicyHolder ?? string.Empty,
                    ContactNumber = n.ContactNumber ?? string.Empty,
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
                    MemberName = m.MemberName ?? string.Empty,
                    RelationshipToCustomer = m.RelationshipToCustomer ?? string.Empty,
                    DateOfBirth = m.DateOfBirth ?? DateTime.MinValue,
                    Gender = m.Gender ?? string.Empty,
                    CoverageAmount = m.CoverageAmount,
                    IsSmoker = m.IsSmoker,
                    HasPreExistingDiseases = m.HasPreExistingDiseases,
                    DiseaseDescription = m.DiseaseDescription,
                    Occupation = m.Occupation ?? string.Empty,
                    IsPrimaryInsured = m.IsPrimaryInsured,
                    CreatedAt = DateTime.UtcNow
                }).ToList();
            }

            // Update nominees — replace all
            if (dto.Nominees != null)
            {
                draft.PolicyNominees = dto.Nominees.Select(n => new PolicyNominee
                {
                    NomineeName = n.NomineeName ?? string.Empty,
                    RelationshipToPolicyHolder = n.RelationshipToPolicyHolder ?? string.Empty,
                    ContactNumber = n.ContactNumber ?? string.Empty,
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

            if (dto.StartDate.Date < DateTime.Today)
                throw new BadRequestException(
                    "Policy start date cannot be in the past");

            if (dto.StartDate.Date > DateTime.UtcNow.Date.AddYears(1))
                throw new BadRequestException(
                    "Start date cannot be more than 1 year in the future");

            if (!plan.IsCoverageUntilAge)
            {
                if (dto.TermYears < plan.MinTermYears ||
                    dto.TermYears > plan.MaxTermYears)
                    throw new BadRequestException(
                        $"Term years must be between {plan.MinTermYears} " +
                        $"and {plan.MaxTermYears}");
            }

            if (members.Count > plan.MaxPolicyMembersAllowed)
                throw new BadRequestException(
                    $"Max {plan.MaxPolicyMembersAllowed} members allowed");

            var primaryCount = members.Count(m => m.IsPrimaryInsured);
            if (primaryCount != 1)
                throw new BadRequestException(
                    "Exactly one primary insured required");

            foreach (var member in members)
            {
                var age = CalculateAge(member.DateOfBirth ?? DateTime.Today);
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
                    m.DateOfBirth ?? DateTime.Today,
                    m.IsSmoker,
                    m.Gender ?? string.Empty,
                    dto.PremiumFrequency));

            // Update draft ? Pending
            draft.PlanId = dto.PlanId;
            draft.StartDate = dto.StartDate;
            draft.TermYears = dto.TermYears;
            draft.EndDate = dto.StartDate.AddYears(dto.TermYears);
            draft.PremiumFrequency = dto.PremiumFrequency;
            draft.TotalPremiumAmount = totalPremium;
            draft.NextDueDate = dto.StartDate;
            draft.Status = PolicyStatus.Pending;

            // Round Robin assignment for Agent at draft submission time
            var draftAgents = (await _userRepository.GetByRoleAsync(UserRole.Agent))
                                .Where(u => u.IsActive)
                                .OrderBy(u => u.Id)
                                .ToList();
            Console.WriteLine($"[PolicyService][SubmitDraft] Active agents found: {draftAgents.Count}");
            if (draftAgents.Any())
            {
                var config = await _systemConfigRepository.GetConfigAsync();
                int nextIndex = (config.LastAgentAssignmentIndex + 1) % draftAgents.Count;
                draft.AgentId = draftAgents[nextIndex].Id;
                config.LastAgentAssignmentIndex = nextIndex;
                _systemConfigRepository.Update(config);
                await _systemConfigRepository.SaveChangesAsync();
                Console.WriteLine($"[PolicyService][SubmitDraft] Assigned AgentId={draft.AgentId} (index={nextIndex})");
            }
            else
            {
                draft.AgentId = null;
                Console.WriteLine("[PolicyService][SubmitDraft] WARNING: No active agents found – policy will be Unassigned.");
            }

            // Notify assigned agent (if any)
            if (draft.AgentId.HasValue)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: draft.AgentId.Value,
                    title: "New Policy Assigned",
                    message: $"A new policy application {draft.PolicyNumber} has been assigned to you for review.",
                    type: NotificationType.General,
                    policyId: draft.Id,
                    claimId: null,
                    paymentId: null);
            }

            // Replace members and nominees
            draft.PolicyMembers = members.Select(m => new PolicyMember
            {
                MemberName = m.MemberName,
                RelationshipToCustomer = m.RelationshipToCustomer,
                DateOfBirth = m.DateOfBirth ?? DateTime.MinValue,
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
                .OrderByDescending(p => p.CreatedAt)
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

        public BonusCalculationResult GetBonusDetails(PolicyAssignment policy, PolicyMember member)
        {
            if (policy.Plan == null || !policy.Plan.HasBonus || policy.Plan.BonusRate <= 0)
                return new BonusCalculationResult();

            var yearsActive = (DateTime.UtcNow - policy.StartDate).Days / 365;

            if (yearsActive <= 0)
                return new BonusCalculationResult();

            var sumAssured = member.CoverageAmount;
            var bonusPerYear = Math.Round(sumAssured * (policy.Plan.BonusRate / 100), 2);
            var totalBonus = Math.Round(bonusPerYear * yearsActive, 2);
            var terminalBonus = Math.Round(totalBonus * (policy.Plan.TerminalBonusRate / 100), 2);
            var totalPayout = Math.Round(sumAssured + totalBonus + terminalBonus, 2);

            return new BonusCalculationResult
            {
                SumAssured = sumAssured,
                BonusPerYear = bonusPerYear,
                YearsActive = yearsActive,
                TotalBonus = totalBonus,
                TerminalBonus = terminalBonus,
                TotalMaturityPayout = totalPayout,
                BonusRate = policy.Plan.BonusRate,
                TerminalBonusRate = policy.Plan.TerminalBonusRate
            };
        }

        public decimal GetCurrentCoverage(PolicyAssignment policy, PolicyMember member)
        {
            if (policy.Plan == null || !policy.Plan.CoverageIncreasing)
                return member.CoverageAmount;

            // Years since policy started
            var years = (DateTime.UtcNow - policy.StartDate).Days / 365;
            if (years <= 0) return member.CoverageAmount;

            // Coverage increases by CoverageIncreaseRate each year (compound)
            var increaseRate = policy.Plan.CoverageIncreaseRate / 100;
            var currentCoverage = member.CoverageAmount * (decimal)Math.Pow((double)(1 + increaseRate), years);

            // Cap at MaxCoverageAmount
            return Math.Min(currentCoverage, policy.Plan.MaxCoverageAmount);
        }

        public async Task<DocumentResponseDto> ReplaceDocumentAsync(int documentId, int userId, IFormFile file)
        {
            var document = await _documentRepository.GetByIdAsync(documentId);
            if (document == null)
                throw new NotFoundException("Document", documentId);

            var policy = await _policyRepository.GetByIdAsync(document.PolicyAssignmentId!.Value);
            if (policy == null)
                throw new NotFoundException("Policy", document.PolicyAssignmentId.Value);

            if (policy.CustomerId != userId)
                throw new ForbiddenException("You can only replace your own documents");

            // Validation: Only allow if policy status is Pending or Rejected or NeedsCorrection
            if (policy.Status != PolicyStatus.Pending && 
                policy.Status != PolicyStatus.Rejected && 
                policy.Status != PolicyStatus.NeedsCorrection)
            {
                throw new BadRequestException($"Cannot replace document for policy with status: {policy.Status}");
            }

            // Replace file on disk
            var oldFullPath = Path.Combine(_environment.WebRootPath, document.FilePath);
            if (File.Exists(oldFullPath))
            {
                File.Delete(oldFullPath);
            }

            var folderPath = Path.GetDirectoryName(oldFullPath)!;
            var ext = Path.GetExtension(file.FileName);
            var uniqueName = $"{document.DocumentCategory}_{policy.Id}_{Guid.NewGuid()}{ext}";
            var newFilePath = Path.Combine(folderPath, uniqueName);

            using (var stream = new FileStream(newFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update document record
            document.FileName = uniqueName;
            var relativePath = Path.Combine(Path.GetDirectoryName(document.FilePath)!, uniqueName).Replace("\\", "/");
            document.FilePath = relativePath;
            document.UploadedAt = DateTime.UtcNow;
            document.Status = DocumentStatus.Pending;

            _documentRepository.Update(document);
            await _documentRepository.SaveChangesAsync();

            // Reset policy status if it was Rejected or NeedsCorrection
            if (policy.Status == PolicyStatus.Rejected || policy.Status == PolicyStatus.NeedsCorrection)
            {
                policy.Status = PolicyStatus.Pending;
                policy.Remarks = $"Document {document.DocumentCategory} replaced by customer. Awaiting re-verification.";
                _policyRepository.Update(policy);
                await _policyRepository.SaveChangesAsync();

                // Notify Agent
                if (policy.AgentId.HasValue)
                {
                    await _notificationService.CreateNotificationAsync(
                        userId: policy.AgentId.Value,
                        title: "Policy Document Replaced",
                        message: $"Customer has replaced {document.DocumentCategory} for policy {policy.PolicyNumber}. Please re-verify.",
                        type: NotificationType.PolicyStatusUpdate,
                        policyId: policy.Id,
                        claimId: null,
                        paymentId: null);
                }
            }

            return new DocumentResponseDto
            {
                Id = document.Id,
                FileName = document.FileName,
                FilePath = document.FilePath,
                DocumentCategory = document.DocumentCategory,
                UploadedByName = "Customer",
                UploadedAt = document.UploadedAt,
                Status = document.Status.ToString()
            };
        }

        public async Task<(byte[] fileBytes, string fileName)> GeneratePolicyApplicationPdfAsync(int policyId, int customerId)
        {
            var policy = await _policyRepository.GetByIdWithDetailsAsync(policyId);
            if (policy == null)
                throw new NotFoundException("Policy", policyId);

            if (policy.CustomerId != customerId)
                throw new ForbiddenException("You can only download summary for your own policies");

            var fileBytes = GenerateApplicationPdfBytes(policy);
            var fileName = $"PolicyApplication_{policy.PolicyNumber}.pdf";

            return (fileBytes, fileName);
        }

        private byte[] GenerateApplicationPdfBytes(PolicyAssignment policy)
        {
            return QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("POLICY APPLICATION SUMMARY")
                            .FontSize(22).Bold().Underline().FontColor(Colors.Pink.Medium);
                        col.Item().AlignCenter().Text("Hartford Insurance Solution")
                            .FontSize(14).Italic();
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    // Content
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        // 1. Basic Policy Info
                        col.Item().Background(Colors.Grey.Lighten4).Padding(8).Text("BASIC POLICY INFORMATION").Bold().FontSize(12);
                        col.Item().PaddingBottom(15).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            AddRow(table, "Policy Number", policy.PolicyNumber);
                            AddRow(table, "Plan Name", policy.Plan?.PlanName ?? "N/A");
                            AddRow(table, "Start Date", policy.StartDate.ToString("dd MMM yyyy"));
                            AddRow(table, "Policy Duration", policy.Plan?.IsCoverageUntilAge == true ? $"Lifelong (Until Age {policy.Plan.CoverageUntilAge})" : $"{policy.TermYears} Years");
                            AddRow(table, "Premium Frequency", policy.PremiumFrequency.ToString());

                            var totalPremium = policy.TotalPremiumAmount;
                            if (totalPremium == 0 && policy.Plan != null && policy.PolicyMembers != null && policy.PolicyMembers.Any())
                            {
                                totalPremium = policy.PolicyMembers.Sum(m =>
                                    CalculatePremium(
                                        policy.Plan.BaseRate,
                                        m.CoverageAmount,
                                        policy.TermYears,
                                        m.DateOfBirth,
                                        m.IsSmoker,
                                        m.Gender,
                                        policy.PremiumFrequency));
                            }
                            AddRow(table, "Total Premium", $"INR {totalPremium:N2}");
                        });

                        // 2. Insured Members
                        col.Item().Background(Colors.Grey.Lighten4).Padding(8).Text("INSURED MEMBERS").Bold().FontSize(12);
                        col.Item().PaddingBottom(15).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3); // Name
                                cols.RelativeColumn(2); // Relation
                                cols.RelativeColumn(2); // Age/Gender
                                cols.RelativeColumn(2); // Coverage
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Name").Bold();
                                header.Cell().Text("Relation").Bold();
                                header.Cell().Text("Details").Bold();
                                header.Cell().Text("Coverage").Bold();
                            });

                            foreach (var member in policy.PolicyMembers ?? new List<PolicyMember>())
                            {
                                table.Cell().PaddingVertical(2).Text(member.MemberName);
                                table.Cell().PaddingVertical(2).Text(member.RelationshipToCustomer);
                                table.Cell().PaddingVertical(2).Text($"{CalculateAge(member.DateOfBirth)}Y / {member.Gender}");
                                table.Cell().PaddingVertical(2).Text($"INR {member.CoverageAmount:N0}");
                            }
                        });

                        // 3. Nominees
                        col.Item().Background(Colors.Grey.Lighten4).Padding(8).Text("NOMINEES / BENEFICIARIES").Bold().FontSize(12);
                        col.Item().PaddingBottom(15).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3); // Name
                                cols.RelativeColumn(2); // Relation
                                cols.RelativeColumn(2); // Contact
                                cols.RelativeColumn(1); // Share
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Name").Bold();
                                header.Cell().Text("Relation").Bold();
                                header.Cell().Text("Contact").Bold();
                                header.Cell().Text("Share").Bold();
                            });

                            foreach (var nominee in policy.PolicyNominees ?? new List<PolicyNominee>())
                            {
                                table.Cell().PaddingVertical(2).Text(nominee.NomineeName);
                                table.Cell().PaddingVertical(2).Text(nominee.RelationshipToPolicyHolder);
                                table.Cell().PaddingVertical(2).Text(nominee.ContactNumber);
                                table.Cell().PaddingVertical(2).Text($"{nominee.SharePercentage}%");
                            }
                        });

                        // 4. Declaration
                        col.Item().PaddingTop(20).Text("DECLARATION").Bold();
                        col.Item().Text("I hereby declare that the information provided in this application is true and complete to the best of my knowledge. I understand that any false statement or omission may lead to rejection of this application or cancellation of the policy.").FontSize(9).Italic();
                    });

                    // Footer
                    page.Footer().AlignCenter().Column(f =>
                    {
                        f.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        f.Item().PaddingTop(5).Text(x =>
                        {
                            x.Span("Generated on ");
                            x.Span(DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss"));
                        });
                    });
                });
            }).GeneratePdf();
        }

        private static void AddRow(QuestPDF.Fluent.TableDescriptor table, string label, string value)
        {
            table.Cell().PaddingVertical(2).Text(label).Bold();
            table.Cell().PaddingVertical(2).Text(value);
        }

        public async Task<ReinstatementQuoteDto> GetReinstatementQuoteAsync(int policyId)
        {
            var policy = await _policyRepository.GetByIdWithPlanAsync(policyId);
            if (policy == null) throw new KeyNotFoundException($"Policy with ID {policyId} not found");

            if (policy.Status != PolicyStatus.Lapsed)
                throw new InvalidOperationException("Only lapsed policies can be reinstated.");

            if (!policy.LapsedDate.HasValue)
                throw new InvalidOperationException("Policy does not have a valid lapsed date.");

            var plan = policy.Plan;
            if (plan == null) throw new InvalidOperationException("Policy plan not found.");

            var today = DateTime.Today;
            var daysSinceLapse = (today - policy.LapsedDate.Value).TotalDays;

            if (daysSinceLapse > plan.ReinstatementDays)
                throw new InvalidOperationException("The reinstatement window for this policy has closed.");

            int missedMonths = (int)Math.Ceiling(daysSinceLapse / 30);
            if (missedMonths < 1) missedMonths = 1; // Minimum 1 month if lapsed

            decimal monthlyPremium = Math.Round(policy.TotalPremiumAmount / 12, 2);
            decimal missedPremiumTotal = monthlyPremium * missedMonths;
            decimal penaltyAmount = plan.ReinstatementPenaltyAmount;
            decimal totalAmountDue = missedPremiumTotal + penaltyAmount;

            return new ReinstatementQuoteDto
            {
                PolicyId = policy.Id,
                PolicyNumber = policy.PolicyNumber,
                MissedMonths = missedMonths,
                MonthlyPremium = monthlyPremium,
                MissedPremiumTotal = missedPremiumTotal,
                PenaltyAmount = penaltyAmount,
                TotalAmountDue = totalAmountDue,
                QuoteValidUntil = today.AddDays(7),
                DaysRemainingToReinstate = plan.ReinstatementDays - (int)daysSinceLapse
            };
        }

        public async Task<string> ReinstatePolicyAsync(int policyId, string paymentReference)
        {
            if (string.IsNullOrWhiteSpace(paymentReference))
                throw new ArgumentException("Payment reference is required for reinstatement.");

            var policy = await _policyRepository.GetByIdWithPlanAsync(policyId);
            if (policy == null) throw new KeyNotFoundException($"Policy with ID {policyId} not found");

            if (policy.Status != PolicyStatus.Lapsed)
                throw new InvalidOperationException("Only lapsed policies can be reinstated.");

            var plan = policy.Plan;
            if (plan == null) throw new InvalidOperationException("Policy plan not found.");

            var today = DateTime.Today;
            var daysSinceLapse = (today - (policy.LapsedDate?.Date ?? today)).TotalDays;

            if (daysSinceLapse > plan.ReinstatementDays)
                throw new InvalidOperationException("The reinstatement window for this policy has closed.");

            // Update Policy status and dates
            policy.Status = PolicyStatus.Active;
            policy.ReinstatedDate = today;
            policy.LapsedDate = null;
            policy.NextDueDate = today.AddMonths(1);
            policy.EndDate = today.AddYears(1);
            policy.Remarks = $"Policy reinstated with payment reference: {paymentReference}";

            _policyRepository.Update(policy);
            await _policyRepository.SaveChangesAsync();

            // Send notification
            await _notificationService.CreateNotificationAsync(
                userId: policy.CustomerId,
                title: "Policy Reinstated",
                message: $"Your policy {policy.PolicyNumber} has been successfully reinstated and is now Active.",
                type: NotificationType.PolicyStatusUpdate,
                policyId: policy.Id,
                claimId: null,
                paymentId: null);

            // Send email
            try
            {
                var customer = policy.Customer;
                if (customer != null)
                {
                    var emailRequest = new EmailRequest
                    {
                        ToEmail = customer.Email,
                        ToName = customer.Name,
                        Subject = $"Policy Reinstated - {policy.PolicyNumber}",
                        HtmlContent = _templateService.GetGenericNotificationTemplate("Policy Reinstated", 
                            $"Your policy {policy.PolicyNumber} has been successfully reinstated using payment reference {paymentReference}. Your next due date is {policy.NextDueDate:dd MMM yyyy}.")
                    };
                    await _emailService.SendEmailAsync(emailRequest);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send reinstatement email: {ex.Message}");
            }

            return policy.PolicyNumber;
        }

        public async Task SendExpiryReminderAsync(int policyId)
        {
            var policy = await _policyRepository.GetByIdWithDetailsAsync(policyId);
            if (policy == null) throw new NotFoundException("Policy", policyId);

            var customer = await _userRepository.GetByIdAsync(policy.CustomerId);
            if (customer == null) throw new NotFoundException("Customer", policy.CustomerId);

            // Send in-app notification
            await _notificationService.CreateNotificationAsync(
                userId: policy.CustomerId,
                title: "Policy Expiry Reminder",
                message: $"Your policy {policy.PolicyNumber} is expiring on {policy.EndDate:dd MMM yyyy}. Please renew within 30 days to avoid coverage gap.",
                type: NotificationType.PremiumReminder,
                policyId: policy.Id,
                claimId: null,
                paymentId: null);

            // Send email
            try
            {
                var body = _templateService.GetPolicyExpiryReminderTemplate(
                    customer.Name, policy.PolicyNumber, policy.EndDate);

                await _emailService.SendEmailAsync(new EmailRequest
                {
                    ToEmail = customer.Email,
                    ToName = customer.Name,
                    Subject = $"Renewal Reminder: Your Policy {policy.PolicyNumber} is expiring soon",
                    HtmlContent = body
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Expiry Reminder Email Failed: {ex.Message}");
            }
        }
    }
}
