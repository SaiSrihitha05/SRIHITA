using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class ClaimServiceTests
    {
        private (InsuranceDbContext db, ClaimService service, Mock<INotificationService> notifyMock, Mock<IEmailService> emailMock) BuildTestContextAndService()
        {
            var dbOptions = new DbContextOptionsBuilder<InsuranceDbContext>()
                .UseInMemoryDatabase($"ClaimServiceTestDb_{Guid.NewGuid()}")
                .ConfigureWarnings(cfg => cfg.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var dbContext = new InsuranceDbContext(dbOptions);

            // Repositories
            var claimRepo = new ClaimRepository(dbContext);
            var policyRepo = new PolicyRepository(dbContext);
            var userRepo = new UserRepository(dbContext);
            var docRepo = new DocumentRepository(dbContext);
            var paymentRepo = new PaymentRepository(dbContext);

            // Mocks
            var notifyMock = new Mock<INotificationService>();
            var emailMock = new Mock<IEmailService>();
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(m => m.WebRootPath).Returns("wwwroot");

            var service = new ClaimService(
                claimRepo,
                policyRepo,
                userRepo,
                docRepo,
                notifyMock.Object,
                emailMock.Object,
                envMock.Object,
                paymentRepo);

            return (dbContext, service, notifyMock, emailMock);
        }

        private async Task<(User customer, User officer, PolicyAssignment policy, PolicyMember member)> SeedBasicData(InsuranceDbContext db)
        {
            var customer = new User { Name = "Customer", Email = "c@t.com", Role = UserRole.Customer, PasswordHash = "h", IsActive = true, Phone = "123" };
            var officer = new User { Name = "Officer", Email = "o@t.com", Role = UserRole.ClaimsOfficer, PasswordHash = "h", IsActive = true, Phone = "456" };
            db.Users.AddRange(customer, officer);

            var plan = new Plan
            {
                PlanName = "Plan",
                PlanType = "Life",
                Description = "Desc",
                GracePeriodDays = 30,
                HasMaturityBenefit = true,
                IsActive = true
            };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            var policy = new PolicyAssignment
            {
                PolicyNumber = "POL-123",
                CustomerId = customer.Id,
                PlanId = plan.Id,
                Status = PolicyStatus.Active,
                StartDate = DateTime.UtcNow.AddMonths(-1),
                EndDate = DateTime.UtcNow.AddYears(1),
                NextDueDate = DateTime.UtcNow.AddDays(15),
                PremiumFrequency = PremiumFrequency.Monthly
            };
            db.PolicyAssignments.Add(policy);
            await db.SaveChangesAsync();

            var member = new PolicyMember
            {
                PolicyAssignmentId = policy.Id,
                MemberName = "Member",
                CoverageAmount = 100000,
                IsPrimaryInsured = true
            };
            db.PolicyMembers.Add(member);

            var payment = new Payment
            {
                PolicyAssignmentId = policy.Id,
                Amount = 1000,
                Status = PaymentStatus.Completed,
                PaymentDate = DateTime.UtcNow,
                TransactionReference = "T1"
            };
            db.Payments.Add(payment);

            await db.SaveChangesAsync();
            return (customer, officer, policy, member);
        }

        [Fact]
        public async Task FileClaimAsync_ShouldCreateClaim_WhenRequestIsValid()
        {
            // Arrange
            var (db, service, _, _) = BuildTestContextAndService();
            var data = await SeedBasicData(db);
            var dto = new FileClaimDto
            {
                PolicyAssignmentId = data.policy.Id,
                PolicyMemberId = data.member.Id,
                ClaimType = ClaimType.Death,
                DeathCertificateNumber = "DC123",
                NomineeName = "Nominee",
                NomineeContact = "999",
                Remarks = "Test claim",
                Documents = new List<IFormFile> { new Mock<IFormFile>().Object }
            };

            // Act
            var result = await service.FileClaimAsync(data.customer.Id, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Submitted", result.Status);
            Assert.Equal(data.member.CoverageAmount, result.ClaimAmount);
        }

        [Fact]
        public async Task FileClaimAsync_ShouldThrowNotFoundException_WhenPolicyDoesNotExist()
        {
            // Arrange
            var (db, service, _, _) = BuildTestContextAndService();
            var data = await SeedBasicData(db);
            var dto = new FileClaimDto { PolicyAssignmentId = 999 };

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() => service.FileClaimAsync(data.customer.Id, dto));
        }

        [Fact]
        public async Task FileClaimAsync_ShouldThrowForbiddenException_WhenPolicyDoesNotBelongToCustomer()
        {
            // Arrange
            var (db, service, _, _) = BuildTestContextAndService();
            var data = await SeedBasicData(db);
            var otherCustomer = new User { Name = "Other", Email = "o2@t.com", Role = UserRole.Customer, PasswordHash = "h", Phone = "789" };
            db.Users.Add(otherCustomer);
            await db.SaveChangesAsync();

            var dto = new FileClaimDto { PolicyAssignmentId = data.policy.Id };

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(() => service.FileClaimAsync(otherCustomer.Id, dto));
        }

        [Fact]
        public async Task FileClaimAsync_ShouldThrowBadRequestException_WhenPolicyIsClosed()
        {
            // Arrange
            var (db, service, _, _) = BuildTestContextAndService();
            var data = await SeedBasicData(db);
            data.policy.Status = PolicyStatus.Closed;
            await db.SaveChangesAsync();

            var dto = new FileClaimDto { PolicyAssignmentId = data.policy.Id };

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => service.FileClaimAsync(data.customer.Id, dto));
        }

        [Fact]
        public async Task FileClaimAsync_ShouldThrowBadRequestException_WhenMemberDoesNotBelongToPolicy()
        {
            // Arrange
            var (db, service, _, _) = BuildTestContextAndService();
            var data = await SeedBasicData(db);
            var dto = new FileClaimDto
            {
                PolicyAssignmentId = data.policy.Id,
                PolicyMemberId = 999 // Invalid member
            };

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => service.FileClaimAsync(data.customer.Id, dto));
        }

        [Fact]
        public async Task FileClaimAsync_ShouldThrowConflictException_WhenMemberHasActiveClaim()
        {
            // Arrange
            var (db, service, _, _) = BuildTestContextAndService();
            var data = await SeedBasicData(db);

            db.Claims.Add(new InsuranceClaim
            {
                PolicyAssignmentId = data.policy.Id,
                PolicyMemberId = data.member.Id,
                Status = ClaimStatus.Submitted,
                ClaimType = ClaimType.Death,
                NomineeName = "N",
                NomineeContact = "C",
                FiledDate = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var dto = new FileClaimDto
            {
                PolicyAssignmentId = data.policy.Id,
                PolicyMemberId = data.member.Id
            };

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(() => service.FileClaimAsync(data.customer.Id, dto));
        }

        [Fact]
        public async Task AssignClaimsOfficerAsync_ShouldUpdateClaimStatus_WhenOfficerIsValid()
        {
            // Arrange
            var (db, service, _, _) = BuildTestContextAndService();
            var data = await SeedBasicData(db);
            var claim = new InsuranceClaim
            {
                PolicyAssignmentId = data.policy.Id,
                PolicyMemberId = data.member.Id,
                Status = ClaimStatus.Submitted,
                NomineeName = "N",
                NomineeContact = "C",
                FiledDate = DateTime.UtcNow,
                ClaimType = ClaimType.Death
            };
            db.Claims.Add(claim);
            await db.SaveChangesAsync();

            var dto = new AssignClaimsOfficerDto { ClaimsOfficerId = data.officer.Id };

            // Act
            await service.AssignClaimsOfficerAsync(claim.Id, dto);

            // Assert
            var updatedClaim = await db.Claims.FindAsync(claim.Id);
            Assert.Equal(ClaimStatus.UnderReview, updatedClaim!.Status);
            Assert.Equal(data.officer.Id, updatedClaim.ClaimsOfficerId);
        }

        [Fact]
        public async Task AssignClaimsOfficerAsync_ShouldThrowBadRequestException_WhenUserIsNotClaimsOfficer()
        {
            // Arrange
            var (db, service, _, _) = BuildTestContextAndService();
            var data = await SeedBasicData(db);
            var claim = new InsuranceClaim { PolicyAssignmentId = data.policy.Id, PolicyMemberId = data.member.Id, NomineeName = "N", NomineeContact = "C", FiledDate = DateTime.UtcNow, ClaimType = ClaimType.Death };
            db.Claims.Add(claim);
            await db.SaveChangesAsync();

            var dto = new AssignClaimsOfficerDto { ClaimsOfficerId = data.customer.Id }; // Not an officer

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => service.AssignClaimsOfficerAsync(claim.Id, dto));
        }

        [Fact]
        public async Task ProcessClaimAsync_ShouldUpdateStatusToSettled_WhenApprovedWithValidAmount()
        {
            // Arrange
            var (db, service, _, _) = BuildTestContextAndService();
            var data = await SeedBasicData(db);
            var claim = new InsuranceClaim
            {
                PolicyAssignmentId = data.policy.Id,
                PolicyMemberId = data.member.Id,
                ClaimAmount = 50000,
                Status = ClaimStatus.UnderReview,
                ClaimsOfficerId = data.officer.Id,
                NomineeName = "N",
                NomineeContact = "C",
                FiledDate = DateTime.UtcNow,
                ClaimType = ClaimType.Death
            };
            db.Claims.Add(claim);
            await db.SaveChangesAsync();

            var dto = new ProcessClaimDto
            {
                Status = ClaimStatus.Settled,
                SettlementAmount = 45000,
                Remarks = "Approved"
            };

            // Act
            var result = await service.ProcessClaimAsync(claim.Id, data.officer.Id, dto);

            // Assert
            Assert.Equal("Settled", result.Status);
            Assert.Equal(45000, result.SettlementAmount);
        }

        [Fact]
        public async Task ProcessClaimAsync_ShouldThrowBadRequestException_WhenSettlementAmountExceedsClaimAmount()
        {
            // Arrange
            var (db, service, _, _) = BuildTestContextAndService();
            var data = await SeedBasicData(db);
            var claim = new InsuranceClaim
            {
                PolicyAssignmentId = data.policy.Id,
                PolicyMemberId = data.member.Id,
                ClaimAmount = 50000,
                Status = ClaimStatus.UnderReview,
                NomineeName = "N",
                NomineeContact = "C",
                FiledDate = DateTime.UtcNow,
                ClaimType = ClaimType.Death
            };
            db.Claims.Add(claim);
            await db.SaveChangesAsync();

            var dto = new ProcessClaimDto
            {
                Status = ClaimStatus.Approved,
                SettlementAmount = 60000 // Exceeds 50000
            };

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => service.ProcessClaimAsync(claim.Id, data.officer.Id, dto));
        }
    }
}
