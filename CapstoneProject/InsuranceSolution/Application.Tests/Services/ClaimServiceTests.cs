using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class ClaimServiceTests
    {
        public class ClaimTestContext
        {
            public InsuranceDbContext Db { get; set; }
            public Mock<INotificationService> MockNote { get; set; }
            public Mock<IEmailService> MockEmail { get; set; }
            public Mock<IEmailTemplateService> MockTemplate { get; set; }
            public Mock<IPdfService> MockPdf { get; set; }
            public Mock<IWebHostEnvironment> MockEnv { get; set; }
            public Mock<IPaymentRepository> MockPayment { get; set; }
            public Mock<ILoanRepository> MockLoan { get; set; }
            public Mock<IPolicyService> MockPolicyService { get; set; }
            public Mock<ISystemConfigRepository> MockConfig { get; set; }
            public ClaimService Service { get; set; }
        }

        private ClaimTestContext BuildTestContextAndService()
        {
            var options = new DbContextOptionsBuilder<InsuranceDbContext>()
                .UseInMemoryDatabase($"ClaimServiceTestDb_{Guid.NewGuid()}")
                .Options;

            var dbContext = new InsuranceDbContext(options);

            var mockNote = new Mock<INotificationService>();
            var mockEmail = new Mock<IEmailService>();
            var mockTemplate = new Mock<IEmailTemplateService>();
            var mockPdf = new Mock<IPdfService>();

            var mockEnv = new Mock<IWebHostEnvironment>();
            var tempPath = Path.Combine(Path.GetTempPath(), "TestUploads");
            Directory.CreateDirectory(tempPath);
            mockEnv.SetupProperty(e => e.ContentRootPath, tempPath);

            var mockPayment = new Mock<IPaymentRepository>();
            var mockLoan = new Mock<ILoanRepository>();
            var mockPolicyService = new Mock<IPolicyService>();
            var mockConfig = new Mock<ISystemConfigRepository>();

            mockConfig.Setup(c => c.GetConfigAsync())
                .ReturnsAsync(new SystemConfig { Id = 1, LastClaimsOfficerIndex = -1 });

            mockLoan.Setup(l => l.GetActiveLoanByPolicyAsync(It.IsAny<int>()))
                .Returns(Task.FromResult<PolicyLoan?>(null));
            mockPolicyService.Setup(s => s.GetCurrentCoverage(It.IsAny<PolicyAssignment>(), It.IsAny<PolicyMember>()))
                .Returns(0);
            mockPolicyService.Setup(s => s.GetBonusDetails(It.IsAny<PolicyAssignment>(), It.IsAny<PolicyMember>()))
                .Returns(new BonusCalculationResult());

            var claimRepo = new Infrastructure.Repositories.ClaimRepository(dbContext);
            var policyRepo = new Infrastructure.Repositories.PolicyRepository(dbContext);
            var userRepo = new Infrastructure.Repositories.UserRepository(dbContext);
            var docRepo = new Infrastructure.Repositories.DocumentRepository(dbContext);

            var service = new ClaimService(
                claimRepo,
                policyRepo,
                userRepo,
                docRepo,
                mockNote.Object,
                mockEmail.Object,
                mockTemplate.Object,
                mockPdf.Object,
                mockEnv.Object,
                mockPayment.Object,
                mockLoan.Object,
                mockPolicyService.Object,
                mockConfig.Object
            );

            return new ClaimTestContext
            {
                Db = dbContext,
                MockNote = mockNote,
                MockEmail = mockEmail,
                MockTemplate = mockTemplate,
                MockPdf = mockPdf,
                MockEnv = mockEnv,
                MockPayment = mockPayment,
                MockLoan = mockLoan,
                MockPolicyService = mockPolicyService,
                MockConfig = mockConfig,
                Service = service
            };
        }

        private async Task<(User customer, Plan plan, PolicyAssignment policy, PolicyMember member)> SeedPolicyAsync(InsuranceDbContext db)
        {
            var customer = new User { Role = UserRole.Customer, Name = "Test", Email = $"test_{Guid.NewGuid()}@test.com" };
            var plan = new Plan { PlanName = $"Plan_{Guid.NewGuid()}", IsActive = true, GracePeriodDays = 30 };
            db.Users.Add(customer);
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            var policy = new PolicyAssignment
            {
                CustomerId = customer.Id,
                Status = PolicyStatus.Active,
                PolicyNumber = $"POL-{Guid.NewGuid().ToString().Substring(0, 8)}",
                PlanId = plan.Id,
                StartDate = DateTime.UtcNow.AddYears(-1),
                EndDate = DateTime.UtcNow.AddYears(10),
                PremiumFrequency = PremiumFrequency.Yearly
            };
            db.PolicyAssignments.Add(policy);
            await db.SaveChangesAsync();

            var member = new PolicyMember { PolicyAssignmentId = policy.Id, MemberName = "Member", IsPrimaryInsured = true };
            db.PolicyMembers.Add(member);
            await db.SaveChangesAsync();

            return (customer, plan, policy, member);
        }

        [Fact]
        public async Task FileClaimAsync_ShouldThrowNotFound_WhenPolicyDoesNotExist()
        {
            var ctx = BuildTestContextAndService();
            var dto = new FileClaimDto { PolicyAssignmentId = 999 };
            try {
                var res = await ctx.Service.FileClaimAsync(1, dto);
                throw new Exception($"Test failed. Returned cleanly: {System.Text.Json.JsonSerializer.Serialize(res)}");
            } catch (NotFoundException) {
                Assert.True(true);
            } catch (Exception ex) {
                throw new Exception($"Test failed. Threw wrong ex: {ex.GetType()} - {ex.Message}");
            }
        }


        [Fact]
        public async Task FileClaimAsync_ShouldThrowForbidden_WhenCustomerDoesNotOwnPolicy()
        {
            var ctx = BuildTestContextAndService();
            var (_, _, policy, _) = await SeedPolicyAsync(ctx.Db);

            var dto = new FileClaimDto { PolicyAssignmentId = policy.Id };
            // Using customerId = policy.CustomerId + 1 to ensure it's different and NOT the owner
            await Assert.ThrowsAsync<ForbiddenException>(() => ctx.Service.FileClaimAsync(policy.CustomerId + 1, dto));
        }

        [Fact]
        public async Task FileClaimAsync_ShouldThrowBadRequest_WhenPolicyLapsed()
        {
            var ctx = BuildTestContextAndService();
            var (customer, _, policy, _) = await SeedPolicyAsync(ctx.Db);
            policy.Status = PolicyStatus.Lapsed;
            await ctx.Db.SaveChangesAsync();

            var dto = new FileClaimDto { PolicyAssignmentId = policy.Id };
            try {
                var res = await ctx.Service.FileClaimAsync(customer.Id, dto);
                throw new Exception($"Test failed. Returned cleanly: {System.Text.Json.JsonSerializer.Serialize(res)}");
            } catch (BadRequestException) {
                Assert.True(true);
            } catch (Exception ex) {
                throw new Exception($"Test failed. Threw wrong ex: {ex.GetType()} - {ex.Message}");
            }
        }

        [Fact]
        public async Task GetMyClaimsAsync_ShouldReturnOnlyCustomerClaims()
        {
            var ctx = BuildTestContextAndService();
            var (c1, _, p1, m1) = await SeedPolicyAsync(ctx.Db);
            var (c2, _, p2, m2) = await SeedPolicyAsync(ctx.Db);

            ctx.Db.Claims.Add(new InsuranceClaim { PolicyAssignmentId = p1.Id, ClaimForMemberId = m1.Id });
            ctx.Db.Claims.Add(new InsuranceClaim { PolicyAssignmentId = p2.Id, ClaimForMemberId = m2.Id });
            await ctx.Db.SaveChangesAsync();

            var claims = await ctx.Service.GetMyClaimsAsync(c1.Id);
            Assert.Single(claims);
            Assert.Equal(p1.Id, claims.First().PolicyAssignmentId);
        }

        [Fact]
        public async Task ProcessClaimAsync_ShouldUpdateStatusAndNotifyCustomer()
        {
            var ctx = BuildTestContextAndService();
            var (customer, _, policy, member) = await SeedPolicyAsync(ctx.Db);

            var claim = new InsuranceClaim { PolicyAssignmentId = policy.Id, ClaimForMemberId = member.Id, Status = ClaimStatus.UnderReview, ClaimAmount = 100000 };
            ctx.Db.Claims.Add(claim);
            await ctx.Db.SaveChangesAsync();

            var dto = new ProcessClaimDto { Status = ClaimStatus.Approved, Remarks = "Test approval", SettlementAmount = 100000 };
            await ctx.Service.ProcessClaimAsync(claim.Id, 10, dto);

            Assert.Equal(ClaimStatus.Approved, claim.Status);
            ctx.MockNote.Verify(n => n.CreateNotificationAsync(customer.Id, It.IsAny<string>(), It.IsAny<string>(), NotificationType.ClaimStatusUpdate, null, claim.Id, null), Times.Once);
        }

        [Fact]
        public async Task ProcessClaimAsync_ShouldClosePolicy_WhenClaimSettled()
        {
            var ctx = BuildTestContextAndService();
            var (_, _, policy, member) = await SeedPolicyAsync(ctx.Db);

            var claim = new InsuranceClaim { PolicyAssignmentId = policy.Id, ClaimForMemberId = member.Id, Status = ClaimStatus.UnderReview, ClaimAmount = 100000 };
            ctx.Db.Claims.Add(claim);
            await ctx.Db.SaveChangesAsync();

            var dto = new ProcessClaimDto { Status = ClaimStatus.Settled, SettlementAmount = 100000 };
            await ctx.Service.ProcessClaimAsync(claim.Id, 10, dto);

            Assert.Equal(PolicyStatus.Closed, policy.Status);
        }

        [Fact]
        public async Task AssignClaimsOfficerAsync_ShouldSucceed_WhenOfficerIsValid()
        {
            var ctx = BuildTestContextAndService();
            var (_, _, policy, member) = await SeedPolicyAsync(ctx.Db);

            var officer = new User { Role = UserRole.ClaimsOfficer, Name = "Officer", Email = "officer@test.com" };
            ctx.Db.Users.Add(officer);
            await ctx.Db.SaveChangesAsync();

            var claim = new InsuranceClaim { Status = ClaimStatus.Submitted, PolicyAssignmentId = policy.Id, ClaimForMemberId = member.Id };
            ctx.Db.Claims.Add(claim);
            await ctx.Db.SaveChangesAsync();

            var dto = new AssignClaimsOfficerDto { ClaimsOfficerId = officer.Id };
            await ctx.Service.AssignClaimsOfficerAsync(claim.Id, dto);

            Assert.Equal(officer.Id, claim.ClaimsOfficerId);
            Assert.Equal(ClaimStatus.UnderReview, claim.Status);
        }

        [Fact]
        public async Task AssignClaimsOfficerAsync_ShouldThrowBadRequest_WhenUserIsNotOfficer()
        {
            var ctx = BuildTestContextAndService();
            var (_, _, policy, member) = await SeedPolicyAsync(ctx.Db);

            var user = new User { Role = UserRole.Customer, Email = "not-officer@test.com" };
            ctx.Db.Users.Add(user);
            await ctx.Db.SaveChangesAsync();

            var claim = new InsuranceClaim { PolicyAssignmentId = policy.Id, ClaimForMemberId = member.Id };
            ctx.Db.Claims.Add(claim);
            await ctx.Db.SaveChangesAsync();

            var dto = new AssignClaimsOfficerDto { ClaimsOfficerId = user.Id };
            await Assert.ThrowsAsync<BadRequestException>(() => ctx.Service.AssignClaimsOfficerAsync(claim.Id, dto));
        }
    }
}
