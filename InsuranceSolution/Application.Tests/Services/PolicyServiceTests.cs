using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Services;
using Application.Tests.Common;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class PolicyServiceTests : ApplicationTestBase
    {
        private readonly PolicyService _policyService;
        private readonly IPolicyRepository _policyRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;

        public PolicyServiceTests()
        {
            _policyRepository = new PolicyRepository(Context);
            _planRepository = new PlanRepository(Context);
            _userRepository = new UserRepository(Context);
            _documentRepository = new DocumentRepository(Context);
            _mockNotificationService = new Mock<INotificationService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();

            _mockEnvironment.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());

            _policyService = new PolicyService(
                _policyRepository,
                _planRepository,
                _userRepository,
                _documentRepository,
                _mockNotificationService.Object,
                _mockEmailService.Object,
                _mockEnvironment.Object);
        }

        private async Task<Plan> SeedPlanAsync(bool isActive = true)
        {
            var plan = new Plan
            {
                PlanName = "Golden Plan " + Guid.NewGuid(),
                IsActive = isActive,
                MinAge = 18,
                MaxAge = 60,
                MinCoverageAmount = 100000,
                MaxCoverageAmount = 1000000,
                MinTermYears = 5,
                MaxTermYears = 20,
                MaxPolicyMembersAllowed = 4,
                MinNominees = 1,
                MaxNominees = 2,
                BaseRate = 10,
                CommissionRate = 5
            };
            await _planRepository.AddAsync(plan);
            await _planRepository.SaveChangesAsync();
            return plan;
        }

        private List<IFormFile> GetMockFiles(int count)
        {
            var files = new List<IFormFile>();
            for (int i = 0; i < count; i++)
            {
                var mockFile = new Mock<IFormFile>();
                var content = "Fake Content";
                var ms = new MemoryStream();
                var writer = new StreamWriter(ms);
                writer.Write(content);
                writer.Flush();
                ms.Position = 0;
                mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
                mockFile.Setup(f => f.FileName).Returns($"test{i}.pdf");
                mockFile.Setup(f => f.Length).Returns(ms.Length);
                files.Add(mockFile.Object);
            }
            return files;
        }

        #region CreatePolicyAsync Tests (5)

        [Fact]
        public async Task CreatePolicyAsync_ValidInput_ReturnsResponse()
        {
            var plan = await SeedPlanAsync();
            var dto = new CreatePolicyDto { PlanId = plan.Id, StartDate = DateTime.UtcNow.AddDays(10), TermYears = 10, PremiumFrequency = PremiumFrequency.Yearly };
            var members = new List<PolicyMemberDto> { new() { MemberName = "M1", IsPrimaryInsured = true, DateOfBirth = DateTime.UtcNow.AddYears(-30), CoverageAmount = 500000, Gender = "Male" } };
            var nominees = new List<PolicyNomineeDto> { new() { NomineeName = "N1", SharePercentage = 100 } };

            var result = await _policyService.CreatePolicyAsync(1, dto, members, nominees, GetMockFiles(2), new List<IFormFile>());

            Assert.NotNull(result);
            Assert.Equal(PolicyStatus.Pending.ToString(), result.Status);
        }

        [Fact]
        public async Task CreatePolicyAsync_InactivePlan_ThrowsBadRequest()
        {
            var plan = await SeedPlanAsync(false);
            var dto = new CreatePolicyDto { PlanId = plan.Id };
            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.CreatePolicyAsync(1, dto, new(), new(), new(), new()));
        }

        [Fact]
        public async Task CreatePolicyAsync_PastStartDate_ThrowsBadRequest()
        {
            var plan = await SeedPlanAsync();
            var dto = new CreatePolicyDto { PlanId = plan.Id, StartDate = DateTime.UtcNow.AddDays(-1) };
            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.CreatePolicyAsync(1, dto, new(), new(), new(), new()));
        }

        [Fact]
        public async Task CreatePolicyAsync_InvalidNomineeShare_ThrowsBadRequest()
        {
            var plan = await SeedPlanAsync();
            var dto = new CreatePolicyDto { PlanId = plan.Id, StartDate = DateTime.UtcNow.AddDays(10), TermYears = 10 };
            var members = new List<PolicyMemberDto> { new() { MemberName = "M1", IsPrimaryInsured = true, DateOfBirth = DateTime.UtcNow.AddYears(-30), CoverageAmount = 500000 } };
            var nominees = new List<PolicyNomineeDto> { new() { NomineeName = "N1", SharePercentage = 50 } }; // Only 50%

            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.CreatePolicyAsync(1, dto, members, nominees, GetMockFiles(2), new()));
        }

        [Fact]
        public async Task CreatePolicyAsync_TooManyMembers_ThrowsBadRequest()
        {
            var plan = await SeedPlanAsync();
            var dto = new CreatePolicyDto { PlanId = plan.Id, StartDate = DateTime.UtcNow.AddDays(10), TermYears = 10 };
            var members = Enumerable.Range(1, 10).Select(i => new PolicyMemberDto { MemberName = $"M{i}", IsPrimaryInsured = i == 1 }).ToList();

            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.CreatePolicyAsync(1, dto, members, new(), new(), new()));
        }

        #endregion

        #region GetPolicyByIdAsync Tests (5)
        [Fact]
        public async Task GetPolicyByIdAsync_ValidId_ReturnsPolicy()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P1", CustomerId = 1, Status = PolicyStatus.Active };
            await _policyRepository.AddAsync(pol); await _policyRepository.SaveChangesAsync();
            var res = await _policyService.GetPolicyByIdAsync(pol.Id);
            Assert.Equal(pol.Id, res.Id);
        }
        [Fact] public async Task GetPolicyByIdAsync_NotFound_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _policyService.GetPolicyByIdAsync(999));
        [Fact] public async Task GetPolicyByIdAsync_ZeroId_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _policyService.GetPolicyByIdAsync(0));
        [Fact]
        public async Task GetPolicyByIdAsync_IncludesMembers()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P-MEM", CustomerId = 1, Status = PolicyStatus.Active, PolicyMembers = new List<PolicyMember> { new() { MemberName = "M" } } };
            await _policyRepository.AddAsync(pol); await _policyRepository.SaveChangesAsync();
            var res = await _policyService.GetPolicyByIdAsync(pol.Id);
            Assert.NotEmpty(res.Members);
        }
        [Fact]
        public async Task GetPolicyByIdAsync_IncludesNominees()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P-NOM", CustomerId = 1, Status = PolicyStatus.Active, PolicyNominees = new List<PolicyNominee> { new() { NomineeName = "N" } } };
            await _policyRepository.AddAsync(pol); await _policyRepository.SaveChangesAsync();
            var res = await _policyService.GetPolicyByIdAsync(pol.Id);
            Assert.NotEmpty(res.Nominees);
        }
        #endregion

        #region GetAllPoliciesAsync Tests (5)
        [Fact] public async Task GetAllPoliciesAsync_NoData_ReturnsEmpty() => Assert.Empty(await _policyService.GetAllPoliciesAsync());
        [Fact]
        public async Task GetAllPoliciesAsync_WithData_ReturnsAll()
        {
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "P1", CustomerId = 1 });
            await _policyRepository.SaveChangesAsync();
            Assert.Single(await _policyService.GetAllPoliciesAsync());
        }
        [Fact]
        public async Task GetAllPoliciesAsync_Multiple_CorrectCount()
        {
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "PA", CustomerId = 1 });
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "PB", CustomerId = 2 });
            await _policyRepository.SaveChangesAsync();
            Assert.Equal(2, (await _policyService.GetAllPoliciesAsync()).Count());
        }
        [Fact] public async Task GetAllPoliciesAsync_ResultTypeIsCorrect() => Assert.IsAssignableFrom<IEnumerable<PolicyResponseDto>>(await _policyService.GetAllPoliciesAsync());
        [Fact]
        public async Task GetAllPoliciesAsync_MappingIsCorrect()
        {
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "PMAP", CustomerId = 1, Status = PolicyStatus.Pending });
            await _policyRepository.SaveChangesAsync();
            var res = (await _policyService.GetAllPoliciesAsync()).First();
            Assert.Equal("Pending", res.Status);
        }
        #endregion

        #region GetMyPoliciesAsync Tests (5)
        [Fact]
        public async Task GetMyPoliciesAsync_ValidCustomerId_ReturnsOnlyMine()
        {
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "Mine", CustomerId = 1 });
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "Other", CustomerId = 2 });
            await _policyRepository.SaveChangesAsync();
            var res = await _policyService.GetMyPoliciesAsync(1);
            Assert.Single(res);
            Assert.Equal("Mine", res.First().PolicyNumber);
        }
        [Fact] public async Task GetMyPoliciesAsync_NoPolicies_ReturnsEmpty() => Assert.Empty(await _policyService.GetMyPoliciesAsync(1));
        [Fact]
        public async Task GetMyPoliciesAsync_MultipleMine_ReturnsAll()
        {
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "M1", CustomerId = 1 });
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "M2", CustomerId = 1 });
            await _policyRepository.SaveChangesAsync();
            Assert.Equal(2, (await _policyService.GetMyPoliciesAsync(1)).Count());
        }
        [Fact]
        public async Task GetMyPoliciesAsync_MappingCheck()
        {
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "M", CustomerId = 1 });
            await _policyRepository.SaveChangesAsync();
            var res = await _policyService.GetMyPoliciesAsync(1);
            Assert.Equal(1, res.First().CustomerId);
        }
        [Fact] public async Task GetMyPoliciesAsync_InvalidCustomerId_ReturnsEmpty() => Assert.Empty(await _policyService.GetMyPoliciesAsync(999));
        #endregion

        #region GetAgentPoliciesAsync Tests (5)
        [Fact]
        public async Task GetAgentPoliciesAsync_ValidAgentId_ReturnsOnlyAssigned()
        {
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "Assigned", AgentId = 1, CustomerId = 1 });
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "Unassigned", AgentId = null, CustomerId = 2 });
            await _policyRepository.SaveChangesAsync();
            var res = await _policyService.GetAgentPoliciesAsync(1);
            Assert.Single(res);
            Assert.Equal("Assigned", res.First().PolicyNumber);
        }
        [Fact] public async Task GetAgentPoliciesAsync_NoPolicies_ReturnsEmpty() => Assert.Empty(await _policyService.GetAgentPoliciesAsync(1));
        [Fact]
        public async Task GetAgentPoliciesAsync_Multiple_ReturnsAll()
        {
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "A1", AgentId = 1, CustomerId = 1 });
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "A2", AgentId = 1, CustomerId = 2 });
            await _policyRepository.SaveChangesAsync();
            Assert.Equal(2, (await _policyService.GetAgentPoliciesAsync(1)).Count());
        }
        [Fact]
        public async Task GetAgentPoliciesAsync_CorrectMapping()
        {
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "A", AgentId = 1, CustomerId = 1 });
            await _policyRepository.SaveChangesAsync();
            var res = await _policyService.GetAgentPoliciesAsync(1);
            Assert.Equal(1, res.First().AgentId);
        }
        [Fact]
        public async Task GetAgentPoliciesAsync_OtherAgentPolicies_Excluded()
        {
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "A1", AgentId = 1, CustomerId = 1 });
            await _policyRepository.AddAsync(new PolicyAssignment { PolicyNumber = "A2", AgentId = 2, CustomerId = 2 });
            await _policyRepository.SaveChangesAsync();
            var res = await _policyService.GetAgentPoliciesAsync(1);
            Assert.Single(res);
        }
        #endregion

        #region UpdatePolicyStatusAsync Tests (5)
        [Fact]
        public async Task UpdatePolicyStatusAsync_Valid_UpdatesStatus()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", CustomerId = 1, Status = PolicyStatus.Pending };
            await _policyRepository.AddAsync(pol);
            await Context.Users.AddAsync(new User { Id = 1, Email = "c@t.com", Name = "C" }); // Seed customer for email mock
            await _policyRepository.SaveChangesAsync();

            await _policyService.UpdatePolicyStatusAsync(pol.Id, new UpdatePolicyStatusDto { Status = PolicyStatus.Active });
            var updated = await _policyRepository.GetByIdAsync(pol.Id);
            Assert.Equal(PolicyStatus.Active, updated!.Status);
            _mockEmailService.Verify(e => e.SendPolicyStatusChangedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
        [Fact] public async Task UpdatePolicyStatusAsync_NotFound_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _policyService.UpdatePolicyStatusAsync(999, new UpdatePolicyStatusDto()));
        [Fact]
        public async Task UpdatePolicyStatusAsync_TriggersNotification()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", CustomerId = 1, Status = PolicyStatus.Pending };
            await _policyRepository.AddAsync(pol);
            await Context.Users.AddAsync(new User { Id = 1, Email = "c@t.com", Name = "C" });
            await _policyRepository.SaveChangesAsync();
            await _policyService.UpdatePolicyStatusAsync(pol.Id, new UpdatePolicyStatusDto { Status = PolicyStatus.Active });
            _mockNotificationService.Verify(n => n.CreateNotificationAsync(1, "Policy Status Updated", It.IsAny<string>(), NotificationType.PolicyStatusUpdate, pol.Id, It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
        }
        [Fact]
        public async Task UpdatePolicyStatusAsync_SetsAssignedDateWhenActive()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", CustomerId = 1, Status = PolicyStatus.Pending };
            await _policyRepository.AddAsync(pol);
            await Context.Users.AddAsync(new User { Id = 1, Email = "c@t.com", Name = "C" });
            await _policyRepository.SaveChangesAsync();
            await _policyService.UpdatePolicyStatusAsync(pol.Id, new UpdatePolicyStatusDto { Status = PolicyStatus.Active });
            var updated = await _policyRepository.GetByIdAsync(pol.Id);
            Assert.NotNull(updated!.AssignedDate);
        }
        [Fact]
        public async Task UpdatePolicyStatusAsync_SameStatus_Works()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", CustomerId = 1, Status = PolicyStatus.Active };
            await _policyRepository.AddAsync(pol);
            await Context.Users.AddAsync(new User { Id = 1, Email = "c@t.com", Name = "C" });
            await _policyRepository.SaveChangesAsync();
            await _policyService.UpdatePolicyStatusAsync(pol.Id, new UpdatePolicyStatusDto { Status = PolicyStatus.Active });
            Assert.Equal(PolicyStatus.Active, (await _policyRepository.GetByIdAsync(pol.Id))!.Status);
        }
        #endregion

        #region AssignAgentAsync Tests (5)
        [Fact]
        public async Task AssignAgentAsync_Valid_AssignsAgent()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", CustomerId = 1 };
            await _policyRepository.AddAsync(pol);
            var agent = new User { Id = 10, Role = UserRole.Agent, Email = "a@t.com", Name = "A", PasswordHash = "h" };
            await Context.Users.AddAsync(agent);
            await Context.SaveChangesAsync();

            await _policyService.AssignAgentAsync(pol.Id, new AssignAgentDto { AgentId = agent.Id });
            var updated = await _policyRepository.GetByIdAsync(pol.Id);
            Assert.Equal(agent.Id, updated!.AgentId);
        }
        [Fact]
        public async Task AssignAgentAsync_InvalidAgentRole_ThrowsBadRequest()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", CustomerId = 1 };
            await _policyRepository.AddAsync(pol);
            var customer = new User { Id = 11, Role = UserRole.Customer, Email = "c@t.com", Name = "C", PasswordHash = "h" }; // Not an agent
            await Context.Users.AddAsync(customer);
            await Context.SaveChangesAsync();
            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.AssignAgentAsync(pol.Id, new AssignAgentDto { AgentId = customer.Id }));
        }
        [Fact] public async Task AssignAgentAsync_NotFoundPolicy_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _policyService.AssignAgentAsync(999, new AssignAgentDto { AgentId = 1 }));
        [Fact]
        public async Task AssignAgentAsync_NotFoundAgent_ThrowsBadRequest()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", CustomerId = 1 };
            await _policyRepository.AddAsync(pol); await _policyRepository.SaveChangesAsync();
            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.AssignAgentAsync(pol.Id, new AssignAgentDto { AgentId = 999 }));
        }
        [Fact]
        public async Task AssignAgentAsync_ReassigningWorks()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", CustomerId = 1, AgentId = 10 };
            await _policyRepository.AddAsync(pol);
            var agent2 = new User { Id = 20, Role = UserRole.Agent, Email = "a2@t.com", Name = "A2", PasswordHash = "h" };
            await Context.Users.AddAsync(agent2);
            await Context.SaveChangesAsync();
            await _policyService.AssignAgentAsync(pol.Id, new AssignAgentDto { AgentId = agent2.Id });
            Assert.Equal(20, (await _policyRepository.GetByIdAsync(pol.Id))!.AgentId);
        }
        #endregion

        #region CancelPendingPolicyAsync Tests (5)
        [Fact]
        public async Task CancelPendingPolicyAsync_ValidPending_Cancels()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", CustomerId = 1, Status = PolicyStatus.Pending };
            await _policyRepository.AddAsync(pol); await _policyRepository.SaveChangesAsync();
            await _policyService.CancelPendingPolicyAsync(pol.Id, 1);
            Assert.Equal(PolicyStatus.Cancelled, (await _policyRepository.GetByIdAsync(pol.Id))!.Status);
        }
        [Fact]
        public async Task CancelPendingPolicyAsync_WrongCustomer_ThrowsForbidden()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", CustomerId = 1, Status = PolicyStatus.Pending };
            await _policyRepository.AddAsync(pol); await _policyRepository.SaveChangesAsync();
            await Assert.ThrowsAsync<ForbiddenException>(() => _policyService.CancelPendingPolicyAsync(pol.Id, 2));
        }
        [Fact]
        public async Task CancelPendingPolicyAsync_NotPending_ThrowsBadRequest()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", CustomerId = 1, Status = PolicyStatus.Active };
            await _policyRepository.AddAsync(pol); await _policyRepository.SaveChangesAsync();
            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.CancelPendingPolicyAsync(pol.Id, 1));
        }
        [Fact] public async Task CancelPendingPolicyAsync_NotFound_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _policyService.CancelPendingPolicyAsync(999, 1));
        [Fact]
        public async Task CancelPendingPolicyAsync_AlreadyCancelled_ThrowsBadRequest()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", CustomerId = 1, Status = PolicyStatus.Cancelled };
            await _policyRepository.AddAsync(pol); await _policyRepository.SaveChangesAsync();
            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.CancelPendingPolicyAsync(pol.Id, 1));
        }
        #endregion

        #region SaveDraftAsync Tests (5)
        [Fact]
        public async Task SaveDraftAsync_Valid_Saves()
        {
            var res = await _policyService.SaveDraftAsync(1, new SaveDraftDto { PlanId = 1 });
            Assert.Equal("Draft", res.Status);
        }
        [Fact]
        public async Task SaveDraftAsync_InactivePlan_ThrowsBadRequest()
        {
            var plan = await SeedPlanAsync(false);
            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.SaveDraftAsync(1, new SaveDraftDto { PlanId = plan.Id }));
        }
        [Fact]
        public async Task SaveDraftAsync_NoPlanId_StillSaves()
        {
            var res = await _policyService.SaveDraftAsync(1, new SaveDraftDto { PlanId = null });
            Assert.Equal("Draft", res.Status);
        }
        [Fact]
        public async Task SaveDraftAsync_WithMembers_SavesMembers()
        {
            var dto = new SaveDraftDto { Members = new List<PolicyMemberDto> { new() { MemberName = "M" } } };
            var res = await _policyService.SaveDraftAsync(1, dto);
            Assert.Single(res.Members);
        }
        [Fact]
        public async Task SaveDraftAsync_WithNominees_SavesNominees()
        {
            var dto = new SaveDraftDto { Nominees = new List<PolicyNomineeDto> { new() { NomineeName = "N", SharePercentage = 100 } } };
            var res = await _policyService.SaveDraftAsync(1, dto);
            Assert.Single(res.Nominees);
        }
        #endregion

        #region UpdateDraftAsync Tests (5)
        [Fact]
        public async Task UpdateDraftAsync_Valid_Updates()
        {
            var dr = await _policyService.SaveDraftAsync(1, new SaveDraftDto { PlanId = 1 });
            var res = await _policyService.UpdateDraftAsync(dr.Id, 1, new SaveDraftDto { TermYears = 15 });
            Assert.Equal(15, res.TermYears);
        }
        [Fact]
        public async Task UpdateDraftAsync_WrongCustomer_ThrowsForbidden()
        {
            var dr = await _policyService.SaveDraftAsync(1, new SaveDraftDto());
            await Assert.ThrowsAsync<ForbiddenException>(() => _policyService.UpdateDraftAsync(dr.Id, 2, new SaveDraftDto()));
        }
        [Fact]
        public async Task UpdateDraftAsync_NotDraftStatus_ThrowsBadRequest()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", Status = PolicyStatus.Pending, CustomerId = 1 };
            await _policyRepository.AddAsync(pol); await _policyRepository.SaveChangesAsync();
            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.UpdateDraftAsync(pol.Id, 1, new SaveDraftDto()));
        }
        [Fact]
        public async Task UpdateDraftAsync_UpdatePlan_ChecksExists()
        {
            var dr = await _policyService.SaveDraftAsync(1, new SaveDraftDto());
            await Assert.ThrowsAsync<NotFoundException>(() => _policyService.UpdateDraftAsync(dr.Id, 1, new SaveDraftDto { PlanId = 999 }));
        }
        [Fact]
        public async Task UpdateDraftAsync_EmptyMembers_ResetsMembers()
        {
            var dr = await _policyService.SaveDraftAsync(1, new SaveDraftDto { Members = new List<PolicyMemberDto> { new() { MemberName = "M" } } });
            var res = await _policyService.UpdateDraftAsync(dr.Id, 1, new SaveDraftDto { Members = new List<PolicyMemberDto>() });
            Assert.Empty(res.Members);
        }
        #endregion

        #region SubmitDraftAsync Tests (5)
        [Fact]
        public async Task SubmitDraftAsync_ValidDraft_ReturnsPending()
        {
            var plan = await SeedPlanAsync();
            var dr = await _policyService.SaveDraftAsync(1, new SaveDraftDto { PlanId = plan.Id });

            var dto = new CreatePolicyDto { PlanId = plan.Id, StartDate = DateTime.UtcNow.AddDays(10), TermYears = 10 };
            var members = new List<PolicyMemberDto> { new() { MemberName = "M", IsPrimaryInsured = true, DateOfBirth = DateTime.UtcNow.AddYears(-30), CoverageAmount = 500000 } };
            var nominees = new List<PolicyNomineeDto> { new() { NomineeName = "N", SharePercentage = 100 } };

            var res = await _policyService.SubmitDraftAsync(dr.Id, 1, dto, members, nominees, GetMockFiles(2), new());
            Assert.Equal("Pending", res.Status);
        }
        [Fact]
        public async Task SubmitDraftAsync_FullValidationFails_ThrowsException()
        {
            var plan = await SeedPlanAsync();
            var dr = await _policyService.SaveDraftAsync(1, new SaveDraftDto { PlanId = plan.Id });
            var dto = new CreatePolicyDto { PlanId = plan.Id, StartDate = DateTime.UtcNow.AddDays(-1) }; // Overdue
            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.SubmitDraftAsync(dr.Id, 1, dto, new(), new(), new(), new()));
        }
        [Fact]
        public async Task SubmitDraftAsync_WrongCustomer_ThrowsForbidden()
        {
            var dr = await _policyService.SaveDraftAsync(1, new SaveDraftDto());
            await Assert.ThrowsAsync<ForbiddenException>(() => _policyService.SubmitDraftAsync(dr.Id, 2, new(), new(), new(), new(), new()));
        }
        [Fact]
        public async Task SubmitDraftAsync_NotDraft_ThrowsBadRequest()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", Status = PolicyStatus.Pending, CustomerId = 1 };
            await _policyRepository.AddAsync(pol); await _policyRepository.SaveChangesAsync();
            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.SubmitDraftAsync(pol.Id, 1, new(), new(), new(), new(), new()));
        }
        [Fact]
        public async Task SubmitDraftAsync_TriggersNotification()
        {
            var plan = await SeedPlanAsync();
            var dr = await _policyService.SaveDraftAsync(1, new SaveDraftDto { PlanId = plan.Id });
            var dto = new CreatePolicyDto { PlanId = plan.Id, StartDate = DateTime.UtcNow.AddDays(10), TermYears = 10 };
            var members = new List<PolicyMemberDto> { new() { MemberName = "M", IsPrimaryInsured = true, DateOfBirth = DateTime.UtcNow.AddYears(-30), CoverageAmount = 500000 } };
            var nominees = new List<PolicyNomineeDto> { new() { NomineeName = "N", SharePercentage = 100 } };
            await _policyService.SubmitDraftAsync(dr.Id, 1, dto, members, nominees, GetMockFiles(2), new());
            _mockNotificationService.Verify(n => n.CreateNotificationAsync(1, "Policy Submitted", It.IsAny<string>(), NotificationType.PolicyStatusUpdate, dr.Id, It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
        }
        #endregion

        #region GetMyDraftsAsync Tests (5)
        [Fact]
        public async Task GetMyDraftsAsync_ReturnsOnlyDrafts()
        {
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { PolicyNumber = "D", CustomerId = 1, Status = PolicyStatus.Draft });
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { PolicyNumber = "P", CustomerId = 1, Status = PolicyStatus.Pending });
            await Context.SaveChangesAsync();
            var res = await _policyService.GetMyDraftsAsync(1);
            Assert.Single(res);
            Assert.Equal("Draft", res.First().Status);
        }
        [Fact] public async Task GetMyDraftsAsync_NoDrafts_ReturnsEmpty() => Assert.Empty(await _policyService.GetMyDraftsAsync(1));
        [Fact]
        public async Task GetMyDraftsAsync_Multiple_ReturnsAll()
        {
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { PolicyNumber = "D1", CustomerId = 1, Status = PolicyStatus.Draft });
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { PolicyNumber = "D2", CustomerId = 1, Status = PolicyStatus.Draft });
            await Context.SaveChangesAsync();
            Assert.Equal(2, (await _policyService.GetMyDraftsAsync(1)).Count());
        }
        [Fact]
        public async Task GetMyDraftsAsync_WrongCustomerDraftsExcluded()
        {
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { PolicyNumber = "D1", CustomerId = 1, Status = PolicyStatus.Draft });
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { PolicyNumber = "D2", CustomerId = 2, Status = PolicyStatus.Draft });
            await Context.SaveChangesAsync();
            Assert.Single(await _policyService.GetMyDraftsAsync(1));
        }
        [Fact] public async Task GetMyDraftsAsync_EmptyResultForInvalidCustomer_ReturnsEmpty() => Assert.Empty(await _policyService.GetMyDraftsAsync(999));
        #endregion

        #region DeleteDraftAsync Tests (5)
        [Fact]
        public async Task DeleteDraftAsync_Valid_Removes()
        {
            var dr = await _policyService.SaveDraftAsync(1, new SaveDraftDto());
            await _policyService.DeleteDraftAsync(dr.Id, 1);
            Assert.Null(await _policyRepository.GetByIdAsync(dr.Id));
        }
        [Fact]
        public async Task DeleteDraftAsync_WrongCustomer_ThrowsForbidden()
        {
            var dr = await _policyService.SaveDraftAsync(1, new SaveDraftDto());
            await Assert.ThrowsAsync<ForbiddenException>(() => _policyService.DeleteDraftAsync(dr.Id, 2));
        }
        [Fact]
        public async Task DeleteDraftAsync_NotDraft_ThrowsBadRequest()
        {
            var pol = new PolicyAssignment { PolicyNumber = "P", Status = PolicyStatus.Pending, CustomerId = 1 };
            await _policyRepository.AddAsync(pol); await _policyRepository.SaveChangesAsync();
            await Assert.ThrowsAsync<BadRequestException>(() => _policyService.DeleteDraftAsync(pol.Id, 1));
        }
        [Fact] public async Task DeleteDraftAsync_NotFound_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _policyService.DeleteDraftAsync(999, 1));
        [Fact]
        public async Task DeleteDraftAsync_AlreadyDeleted_ThrowsNotFound()
        {
            var dr = await _policyService.SaveDraftAsync(1, new SaveDraftDto());
            await _policyService.DeleteDraftAsync(dr.Id, 1);
            await Assert.ThrowsAsync<NotFoundException>(() => _policyService.DeleteDraftAsync(dr.Id, 1));
        }
        #endregion
    }
}
