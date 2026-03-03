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
    public class ClaimServiceTests : ApplicationTestBase
    {
        private readonly ClaimService _claimService;
        private readonly IClaimRepository _claimRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IUserRepository _userRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;

        public ClaimServiceTests()
        {
            _claimRepository = new ClaimRepository(Context);
            _policyRepository = new PolicyRepository(Context);
            _userRepository = new UserRepository(Context);
            _documentRepository = new DocumentRepository(Context);
            _paymentRepository = new PaymentRepository(Context);
            _mockNotificationService = new Mock<INotificationService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();

            _mockEnvironment.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());

            _claimService = new ClaimService(
                _claimRepository,
                _policyRepository,
                _userRepository,
                _documentRepository,
                _mockNotificationService.Object,
                _mockEmailService.Object,
                _mockEnvironment.Object,
                _paymentRepository);
        }

        private async Task<(User customer, Plan plan, PolicyAssignment policy, PolicyMember member)> SeedBaseDataAsync()
        {
            var customer = new User { Name = "C", Email = "c@t.com", Role = UserRole.Customer, PasswordHash = "h" };
            await Context.Users.AddAsync(customer);

            var plan = new Plan
            {
                PlanName = "Plan",
                IsActive = true,
                MinAge = 0,
                MaxAge = 100,
                MinCoverageAmount = 1000,
                MaxCoverageAmount = 1000000,
                MinTermYears = 1,
                MaxTermYears = 50,
                GracePeriodDays = 30,
                HasMaturityBenefit = true,
                BaseRate = 10
            };
            await Context.Plans.AddAsync(plan);

            var policy = new PolicyAssignment
            {
                PolicyNumber = "POL123",
                CustomerId = customer.Id,
                PlanId = plan.Id,
                Status = PolicyStatus.Active,
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow.AddYears(10),
                NextDueDate = DateTime.UtcNow.AddMonths(1)
            };
            await Context.PolicyAssignments.AddAsync(policy);

            var member = new PolicyMember
            {
                MemberName = "Primary",
                IsPrimaryInsured = true,
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                CoverageAmount = 500000,
                PolicyAssignmentId = policy.Id
            };
            await Context.PolicyMembers.AddAsync(member);

            // Add a payment to allow filing
            await Context.Payments.AddAsync(new Payment
            {
                PolicyAssignmentId = policy.Id,
                Status = PaymentStatus.Completed,
                Amount = 1000,
                PaymentDate = DateTime.UtcNow.AddDays(-1)
            });

            await Context.SaveChangesAsync();
            return (customer, plan, policy, member);
        }

        private List<IFormFile> GetMockFiles(int count)
        {
            var files = new List<IFormFile>();
            for (int i = 0; i < count; i++)
            {
                var mockFile = new Mock<IFormFile>();
                var ms = new MemoryStream();
                var writer = new StreamWriter(ms);
                writer.Write("Test"); writer.Flush(); ms.Position = 0;
                mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
                mockFile.Setup(f => f.FileName).Returns($"t{i}.pdf");
                mockFile.Setup(f => f.Length).Returns(ms.Length);
                files.Add(mockFile.Object);
            }
            return files;
        }

        #region FileClaimAsync Tests (5)

        [Fact]
        public async Task FileClaimAsync_ValidInput_ReturnsResponse()
        {
            var data = await SeedBaseDataAsync();
            var dto = new FileClaimDto
            {
                PolicyAssignmentId = data.policy.Id,
                PolicyMemberId = data.member.Id,
                ClaimType = ClaimType.Accidental,
                Remarks = "Accident"
            };

            var result = await _claimService.FileClaimAsync(data.customer.Id, dto);

            Assert.NotNull(result);
            Assert.Equal(ClaimStatus.Submitted.ToString(), result.Status);
        }

        [Fact]
        public async Task FileClaimAsync_NoPayments_ThrowsBadRequest()
        {
            var data = await SeedBaseDataAsync();
            var p = await Context.Payments.FirstAsync(p => p.PolicyAssignmentId == data.policy.Id);
            Context.Payments.Remove(p); await Context.SaveChangesAsync();

            var dto = new FileClaimDto { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id };
            await Assert.ThrowsAsync<BadRequestException>(() => _claimService.FileClaimAsync(data.customer.Id, dto));
        }

        [Fact]
        public async Task FileClaimAsync_MaturityType_ThrowsBadRequest()
        {
            var data = await SeedBaseDataAsync();
            var dto = new FileClaimDto { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id, ClaimType = ClaimType.Maturity };
            await Assert.ThrowsAsync<BadRequestException>(() => _claimService.FileClaimAsync(data.customer.Id, dto));
        }

        [Fact]
        public async Task FileClaimAsync_DeathClaimMissingDocs_ThrowsBadRequest()
        {
            var data = await SeedBaseDataAsync();
            var dto = new FileClaimDto { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id, ClaimType = ClaimType.Death, DeathCertificateNumber = "D123", NomineeName = "N", NomineeContact = "1", Documents = null };
            await Assert.ThrowsAsync<BadRequestException>(() => _claimService.FileClaimAsync(data.customer.Id, dto));
        }

        [Fact]
        public async Task FileClaimAsync_ActivePolicyCheck_ThrowsBadRequestIfPending()
        {
            var data = await SeedBaseDataAsync();
            var pol = await Context.PolicyAssignments.FindAsync(data.policy.Id);
            pol!.Status = PolicyStatus.Pending; await Context.SaveChangesAsync();

            var dto = new FileClaimDto { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id };
            await Assert.ThrowsAsync<BadRequestException>(() => _claimService.FileClaimAsync(data.customer.Id, dto));
        }

        #endregion

        #region AssignClaimsOfficerAsync Tests (5)
        [Fact]
        public async Task AssignClaimsOfficerAsync_ValidOfficer_Assigns()
        {
            var data = await SeedBaseDataAsync();
            var cl = new InsuranceClaim { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id, Status = ClaimStatus.Submitted };
            await Context.Claims.AddAsync(cl);
            var officer = new User { Role = UserRole.ClaimsOfficer, Email = "o@t.com", PasswordHash = "h" };
            await Context.Users.AddAsync(officer); await Context.SaveChangesAsync();

            await _claimService.AssignClaimsOfficerAsync(cl.Id, new AssignClaimsOfficerDto { ClaimsOfficerId = officer.Id });
            var updated = await Context.Claims.FindAsync(cl.Id);
            Assert.Equal(officer.Id, updated!.ClaimsOfficerId);
            Assert.Equal(ClaimStatus.UnderReview, updated.Status);
        }
        [Fact] public async Task AssignClaimsOfficerAsync_NotFound_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _claimService.AssignClaimsOfficerAsync(999, new AssignClaimsOfficerDto { ClaimsOfficerId = 1 }));
        [Fact]
        public async Task AssignClaimsOfficerAsync_InvalidRole_ThrowsBadRequest()
        {
            var data = await SeedBaseDataAsync();
            var cl = new InsuranceClaim { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id };
            await Context.Claims.AddAsync(cl);
            var customer = new User { Role = UserRole.Customer, Email = "c2@t.com", PasswordHash = "h" };
            await Context.Users.AddAsync(customer); await Context.SaveChangesAsync();
            await Assert.ThrowsAsync<BadRequestException>(() => _claimService.AssignClaimsOfficerAsync(cl.Id, new AssignClaimsOfficerDto { ClaimsOfficerId = customer.Id }));
        }
        [Fact]
        public async Task AssignClaimsOfficerAsync_TriggersNotification()
        {
            var data = await SeedBaseDataAsync();
            var cl = new InsuranceClaim { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id, Status = ClaimStatus.Submitted };
            await Context.Claims.AddAsync(cl);
            var officer = new User { Role = UserRole.ClaimsOfficer, Email = "o@t.com", PasswordHash = "h" };
            await Context.Users.AddAsync(officer); await Context.SaveChangesAsync();
            await _claimService.AssignClaimsOfficerAsync(cl.Id, new AssignClaimsOfficerDto { ClaimsOfficerId = officer.Id });
            _mockNotificationService.Verify(n => n.CreateNotificationAsync(data.customer.Id, "Claim Under Review", It.IsAny<string>(), NotificationType.ClaimStatusUpdate, It.IsAny<int?>(), cl.Id, It.IsAny<int?>()), Times.Once);
        }
        [Fact]
        public async Task AssignClaimsOfficerAsync_OfficerNotificationCheck()
        {
            var data = await SeedBaseDataAsync();
            var cl = new InsuranceClaim { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id, Status = ClaimStatus.Submitted };
            await Context.Claims.AddAsync(cl);
            var officer = new User { Role = UserRole.ClaimsOfficer, Email = "o@t.com", PasswordHash = "h" };
            await Context.Users.AddAsync(officer); await Context.SaveChangesAsync();
            await _claimService.AssignClaimsOfficerAsync(cl.Id, new AssignClaimsOfficerDto { ClaimsOfficerId = officer.Id });
            _mockNotificationService.Verify(n => n.CreateNotificationAsync(officer.Id, "New Claim Assigned", It.IsAny<string>(), NotificationType.ClaimStatusUpdate, It.IsAny<int?>(), cl.Id, It.IsAny<int?>()), Times.Once);
        }
        #endregion

        #region ProcessClaimAsync Tests (5)

        [Fact]
        public async Task ProcessClaimAsync_ValidApproval_SetsSettlement()
        {
            var data = await SeedBaseDataAsync();
            var cl = new InsuranceClaim { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id, ClaimAmount = 500000 };
            await Context.Claims.AddAsync(cl); await Context.SaveChangesAsync();

            var dto = new ProcessClaimDto { Status = ClaimStatus.Settled, SettlementAmount = 450000, Remarks = "OK" };
            var result = await _claimService.ProcessClaimAsync(cl.Id, 0, dto);

            Assert.Equal(ClaimStatus.Settled.ToString(), result.Status);
            Assert.Equal(450000, result.SettlementAmount);

            var pol = await Context.PolicyAssignments.FindAsync(data.policy.Id);
            Assert.Equal(PolicyStatus.Closed, pol!.Status);
        }

        [Fact]
        public async Task ProcessClaimAsync_AmountTooHigh_ThrowsBadRequest()
        {
            var data = await SeedBaseDataAsync();
            var cl = new InsuranceClaim { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id, ClaimAmount = 1000 };
            await Context.Claims.AddAsync(cl); await Context.SaveChangesAsync();
            var dto = new ProcessClaimDto { Status = ClaimStatus.Approved, SettlementAmount = 2000 };
            await Assert.ThrowsAsync<BadRequestException>(() => _claimService.ProcessClaimAsync(cl.Id, 0, dto));
        }

        [Fact]
        public async Task ProcessClaimAsync_Rejection_SetsZeroSettlement()
        {
            var data = await SeedBaseDataAsync();
            var cl = new InsuranceClaim { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id, ClaimAmount = 1000 };
            await Context.Claims.AddAsync(cl); await Context.SaveChangesAsync();
            var dto = new ProcessClaimDto { Status = ClaimStatus.Rejected, Remarks = "Denied" };
            var res = await _claimService.ProcessClaimAsync(cl.Id, 0, dto);
            Assert.Equal(0, res.SettlementAmount);
        }

        [Fact]
        public async Task ProcessClaimAsync_MissingAmountOnSettle_ThrowsBadRequest()
        {
            var data = await SeedBaseDataAsync();
            var cl = new InsuranceClaim { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id };
            await Context.Claims.AddAsync(cl); await Context.SaveChangesAsync();
            var dto = new ProcessClaimDto { Status = ClaimStatus.Settled, SettlementAmount = null };
            await Assert.ThrowsAsync<BadRequestException>(() => _claimService.ProcessClaimAsync(cl.Id, 0, dto));
        }

        [Fact]
        public async Task ProcessClaimAsync_TriggersEmail()
        {
            var data = await SeedBaseDataAsync();
            var cl = new InsuranceClaim { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id };
            await Context.Claims.AddAsync(cl); await Context.SaveChangesAsync();
            await _claimService.ProcessClaimAsync(cl.Id, 0, new ProcessClaimDto { Status = ClaimStatus.Rejected });
            _mockEmailService.Verify(e => e.SendPolicyStatusChangedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "Rejected"), Times.Once);
        }

        #endregion

        #region ProcessMaturityClaimsAsync Tests (5)
        [Fact]
        public async Task ProcessMaturityClaimsAsync_CreatesAutoClaims()
        {
            var data = await SeedBaseDataAsync();
            var pol = await Context.PolicyAssignments.FindAsync(data.policy.Id);
            pol!.EndDate = DateTime.UtcNow.AddDays(-1); await Context.SaveChangesAsync();

            await _claimService.ProcessMaturityClaimsAsync();
            var cl = await Context.Claims.FirstOrDefaultAsync(c => c.ClaimType == ClaimType.Maturity);
            Assert.NotNull(cl);
            Assert.Equal(ClaimStatus.Approved, cl.Status);
        }
        [Fact]
        public async Task ProcessMaturityClaimsAsync_UpdatesPolicyStatus()
        {
            var data = await SeedBaseDataAsync();
            var pol = await Context.PolicyAssignments.FindAsync(data.policy.Id);
            pol!.EndDate = DateTime.UtcNow.AddDays(-1); await Context.SaveChangesAsync();
            await _claimService.ProcessMaturityClaimsAsync();
            var updatedPol = await Context.PolicyAssignments.FindAsync(data.policy.Id);
            Assert.Equal(PolicyStatus.Matured, updatedPol!.Status);
        }
        [Fact]
        public async Task ProcessMaturityClaimsAsync_NoBenefitPlan_ExpiresPolicy()
        {
            var data = await SeedBaseDataAsync();
            var plan = await Context.Plans.FindAsync(data.plan.Id);
            plan!.HasMaturityBenefit = false;
            var pol = await Context.PolicyAssignments.FindAsync(data.policy.Id);
            pol!.EndDate = DateTime.UtcNow.AddDays(-1); await Context.SaveChangesAsync();
            await _claimService.ProcessMaturityClaimsAsync();
            var updatedPol = await Context.PolicyAssignments.FindAsync(data.policy.Id);
            Assert.Equal(PolicyStatus.Expired, updatedPol!.Status);
            Assert.Empty(await Context.Claims.ToListAsync());
        }
        [Fact]
        public async Task ProcessMaturityClaimsAsync_SkipsIfActiveClaimExists()
        {
            var data = await SeedBaseDataAsync();
            await Context.Claims.AddAsync(new InsuranceClaim { PolicyMemberId = data.member.Id, Status = ClaimStatus.Submitted });
            var pol = await Context.PolicyAssignments.FindAsync(data.policy.Id);
            pol!.EndDate = DateTime.UtcNow.AddDays(-1); await Context.SaveChangesAsync();
            await _claimService.ProcessMaturityClaimsAsync();
            Assert.DoesNotContain(await Context.Claims.ToListAsync(), c => c.ClaimType == ClaimType.Maturity);
        }
        [Fact]
        public async Task ProcessMaturityClaimsAsync_TriggersNotification()
        {
            var data = await SeedBaseDataAsync();
            var pol = await Context.PolicyAssignments.FindAsync(data.policy.Id);
            pol!.EndDate = DateTime.UtcNow.AddDays(-1); await Context.SaveChangesAsync();
            await _claimService.ProcessMaturityClaimsAsync();
            _mockNotificationService.Verify(n => n.CreateNotificationAsync(data.customer.Id, "Policy Matured \u2014 Benefit Credited", It.IsAny<string>(), NotificationType.PolicyStatusUpdate, It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
        }
        #endregion

        #region GetAllClaimsAsync Tests (5)
        [Fact] public async Task GetAllClaimsAsync_NoData_ReturnsEmpty() => Assert.Empty(await _claimService.GetAllClaimsAsync());
        [Fact]
        public async Task GetAllClaimsAsync_WithData_ReturnsAll()
        {
            await Context.Claims.AddAsync(new InsuranceClaim()); await Context.SaveChangesAsync();
            Assert.Single(await _claimService.GetAllClaimsAsync());
        }
        [Fact]
        public async Task GetAllClaimsAsync_CountCorrect()
        {
            await Context.Claims.AddAsync(new InsuranceClaim()); await Context.Claims.AddAsync(new InsuranceClaim());
            await Context.SaveChangesAsync();
            Assert.Equal(2, (await _claimService.GetAllClaimsAsync()).Count());
        }
        [Fact]
        public async Task GetAllClaimsAsync_MappingCheck()
        {
            var c = new InsuranceClaim { ClaimAmount = 100 }; await Context.Claims.AddAsync(c); await Context.SaveChangesAsync();
            var res = await _claimService.GetAllClaimsAsync();
            Assert.Equal(100, res.First().ClaimAmount);
        }
        [Fact] public async Task GetAllClaimsAsync_ResultTypeIsClaimResponseDto() => Assert.IsAssignableFrom<IEnumerable<ClaimResponseDto>>(await _claimService.GetAllClaimsAsync());
        #endregion

        #region GetMyClaimsAsync Tests (5)
        [Fact]
        public async Task GetMyClaimsAsync_FilterByCustomer_Works()
        {
            var data = await SeedBaseDataAsync();
            await Context.Claims.AddAsync(new InsuranceClaim { PolicyAssignmentId = data.policy.Id });
            await Context.Claims.AddAsync(new InsuranceClaim { PolicyAssignmentId = 999 }); // Not mine
            await Context.SaveChangesAsync();
            var res = await _claimService.GetMyClaimsAsync(data.customer.Id);
            Assert.Single(res);
        }
        [Fact] public async Task GetMyClaimsAsync_NoClaims_ReturnsEmpty() => Assert.Empty(await _claimService.GetMyClaimsAsync(1));
        [Fact]
        public async Task GetMyClaimsAsync_MultipleMyClaims_ReturnsAll()
        {
            var data = await SeedBaseDataAsync();
            await Context.Claims.AddAsync(new InsuranceClaim { PolicyAssignmentId = data.policy.Id });
            await Context.Claims.AddAsync(new InsuranceClaim { PolicyAssignmentId = data.policy.Id });
            await Context.SaveChangesAsync();
            Assert.Equal(2, (await _claimService.GetMyClaimsAsync(data.customer.Id)).Count());
        }
        [Fact]
        public async Task GetMyClaimsAsync_InactiveClaims_StillIncluded()
        {
            var data = await SeedBaseDataAsync();
            await Context.Claims.AddAsync(new InsuranceClaim { PolicyAssignmentId = data.policy.Id, Status = ClaimStatus.Rejected });
            await Context.SaveChangesAsync();
            Assert.Single(await _claimService.GetMyClaimsAsync(data.customer.Id));
        }
        [Fact] public async Task GetMyClaimsAsync_InvalidCustomer_ReturnsEmpty() => Assert.Empty(await _claimService.GetMyClaimsAsync(999));
        #endregion

        #region GetMyAssignedClaimsAsync Tests (5)
        [Fact]
        public async Task GetMyAssignedClaimsAsync_FilterByOfficer_Works()
        {
            await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = 10 });
            await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = 20 });
            await Context.SaveChangesAsync();
            var res = await _claimService.GetMyAssignedClaimsAsync(10);
            Assert.Single(res);
        }
        [Fact] public async Task GetMyAssignedClaimsAsync_NoAssigned_ReturnsEmpty() => Assert.Empty(await _claimService.GetMyAssignedClaimsAsync(1));
        [Fact]
        public async Task GetMyAssignedClaimsAsync_CountIsCorrect()
        {
            await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = 10 });
            await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = 10 });
            await Context.SaveChangesAsync();
            Assert.Equal(2, (await _claimService.GetMyAssignedClaimsAsync(10)).Count());
        }
        [Fact]
        public async Task GetMyAssignedClaimsAsync_UnassignedExcluded()
        {
            await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = null });
            await Context.SaveChangesAsync();
            Assert.Empty(await _claimService.GetMyAssignedClaimsAsync(10));
        }
        [Fact]
        public async Task GetMyAssignedClaimsAsync_MappingOk()
        {
            await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = 10, Status = ClaimStatus.UnderReview });
            await Context.SaveChangesAsync();
            var res = await _claimService.GetMyAssignedClaimsAsync(10);
            Assert.Equal("UnderReview", res.First().Status);
        }
        #endregion

        #region GetClaimByIdAsync Tests (5)
        [Fact]
        public async Task GetClaimByIdAsync_ValidId_ReturnsClaim()
        {
            var c = new InsuranceClaim(); await Context.Claims.AddAsync(c); await Context.SaveChangesAsync();
            var res = await _claimService.GetClaimByIdAsync(c.Id);
            Assert.Equal(c.Id, res.Id);
        }
        [Fact] public async Task GetClaimByIdAsync_NotFound_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _claimService.GetClaimByIdAsync(999));
        [Fact] public async Task GetClaimByIdAsync_ZeroId_ThrowsNotFound() => await Assert.ThrowsAsync<NotFoundException>(() => _claimService.GetClaimByIdAsync(0));
        [Fact]
        public async Task GetClaimByIdAsync_IncludesPolicyInfo()
        {
            var data = await SeedBaseDataAsync();
            var c = new InsuranceClaim { PolicyAssignmentId = data.policy.Id };
            await Context.Claims.AddAsync(c); await Context.SaveChangesAsync();
            var res = await _claimService.GetClaimByIdAsync(c.Id);
            Assert.Equal(data.policy.PolicyNumber, res.PolicyNumber);
        }
        [Fact]
        public async Task GetClaimByIdAsync_IncludesMemberName()
        {
            var data = await SeedBaseDataAsync();
            var c = new InsuranceClaim { PolicyMemberId = data.member.Id };
            await Context.Claims.AddAsync(c); await Context.SaveChangesAsync();
            var res = await _claimService.GetClaimByIdAsync(c.Id);
            Assert.Equal(data.member.MemberName, res.PolicyMemberName);
        }
        #endregion
    }
}
