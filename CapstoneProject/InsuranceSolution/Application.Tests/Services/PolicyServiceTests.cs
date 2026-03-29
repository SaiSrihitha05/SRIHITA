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
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class PolicyServiceTests
    {
        private (InsuranceDbContext db, PolicyService service, Mock<INotificationService> mockNotify, Mock<IEmailService> mockEmail, Mock<IWebHostEnvironment> mockEnv, Mock<ISystemConfigRepository> mockConfig) BuildTestContextAndService()
        {
            var dbOptions = new DbContextOptionsBuilder<InsuranceDbContext>()
                .UseInMemoryDatabase($"PolicyServiceTestDb_{Guid.NewGuid()}")
                .Options;

            var dbContext = new InsuranceDbContext(dbOptions);
            var policyRepo = new PolicyRepository(dbContext);
            var planRepo = new PlanRepository(dbContext);
            var userRepo = new UserRepository(dbContext);
            var docRepo = new DocumentRepository(dbContext);

            var mockNotify = new Mock<INotificationService>();
            var mockEmail = new Mock<IEmailService>();
            var mockTemplate = new Mock<IEmailTemplateService>();
            var mockPdf = new Mock<IPdfService>();
            var mockEnv = new Mock<IWebHostEnvironment>();

            // Set up a mock root path for uploads
            mockEnv.Setup(env => env.WebRootPath).Returns(Path.Combine(Path.GetTempPath(), "TestWebRoot"));

            var mockConfig = new Mock<ISystemConfigRepository>();
            mockConfig.Setup(c => c.GetConfigAsync())
                .ReturnsAsync(new SystemConfig { Id = 1, LastAgentAssignmentIndex = -1 });

            var mockChatRepo = new Mock<IChatMessageRepository>();
            mockChatRepo.Setup(r => r.GetBySessionAsync(It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<ChatMessage>());

            var service = new PolicyService(
                policyRepository: policyRepo,
                planRepository: planRepo,
                userRepository: userRepo,
                documentRepository: docRepo,
                notificationService: mockNotify.Object,
                emailService: mockEmail.Object,
                templateService: mockTemplate.Object,
                pdfService: mockPdf.Object,
                environment: mockEnv.Object,
                systemConfigRepository: mockConfig.Object,
                chatRepo: mockChatRepo.Object);

            return (dbContext, service, mockNotify, mockEmail, mockEnv, mockConfig);
        }

        private async Task<(User customer, Plan plan)> SeedBasicData(InsuranceDbContext db)
        {
            var customer = new User { Name = "Cust", Email = "c@test.com", Role = UserRole.Customer, IsActive = true };
            db.Users.Add(customer);

            var plan = new Plan
            {
                PlanName = "PlanA",
                IsActive = true,
                MinAge = 18,
                MaxAge = 65,
                MinCoverageAmount = 1000,
                MaxCoverageAmount = 10000,
                MinTermYears = 1,
                MaxTermYears = 10,
                MinNominees = 1,
                MaxNominees = 3,
                MaxPolicyMembersAllowed = 4,
                BaseRate = 50
            };
            db.Plans.Add(plan);
            await db.SaveChangesAsync();

            return (customer, plan);
        }

        private List<PolicyMemberDto> GetValidMembers() => new()
        {
            new PolicyMemberDto { MemberName = "Primary", DateOfBirth = DateTime.Today.AddYears(-30), IsPrimaryInsured = true, CoverageAmount = 5000 }
        };

        private List<PolicyNomineeDto> GetValidNominees() => new()
        {
            new PolicyNomineeDto { NomineeName = "Nominee1", SharePercentage = 100 }
        };

        // --- 1. CreatePolicyAsync (3 Tests) ---

        [Fact]
        public async Task CreatePolicyAsync_ShouldCreatePolicySuccessfully_WhenDataIsValid()
        {
            var (db, service, _, _, _, _) = BuildTestContextAndService();
            var (customer, plan) = await SeedBasicData(db);

            var dto = new CreatePolicyDto { PlanId = plan.Id, StartDate = DateTime.Today.AddDays(1), TermYears = 5, PremiumFrequency = PremiumFrequency.Monthly };

            var result = await service.CreatePolicyAsync(customer.Id, dto, GetValidMembers(), GetValidNominees(), new List<IFormFile>(), new List<IFormFile>());

            Assert.NotNull(result);
            Assert.Equal("Pending", result.Status);
            var policy = await db.PolicyAssignments.FirstOrDefaultAsync();
            Assert.NotNull(policy);
            Assert.Equal(5, policy.TermYears);
        }

        [Fact]
        public async Task CreatePolicyAsync_ShouldThrowBadRequestException_WhenTermYearsOutsidePlanLimits()
        {
            var (db, service, _, _, _, _) = BuildTestContextAndService();
            var (customer, plan) = await SeedBasicData(db);

            var dto = new CreatePolicyDto { PlanId = plan.Id, StartDate = DateTime.Today.AddDays(1), TermYears = 20 }; // Max is 10

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.CreatePolicyAsync(customer.Id, dto, GetValidMembers(), GetValidNominees(), new List<IFormFile>(), new List<IFormFile>()));
            Assert.Contains("Term years must be between", ex.Message);
        }

        [Fact]
        public async Task CreatePolicyAsync_ShouldThrowBadRequestException_WhenNomineeSharesDoNotTotal100()
        {
            var (db, service, _, _, _, _) = BuildTestContextAndService();
            var (customer, plan) = await SeedBasicData(db);

            var dto = new CreatePolicyDto { PlanId = plan.Id, StartDate = DateTime.Today.AddDays(1), TermYears = 5 };
            var badNominees = new List<PolicyNomineeDto> { new PolicyNomineeDto { NomineeName = "Nominee", SharePercentage = 50 } }; // Total 50 != 100

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.CreatePolicyAsync(customer.Id, dto, GetValidMembers(), badNominees, new List<IFormFile>(), new List<IFormFile>()));
            Assert.Contains("total 100", ex.Message);
        }

        // --- 2. GetPolicyByIdAsync (3 Tests) ---

        [Fact]
        public async Task GetPolicyByIdAsync_ShouldReturnPolicy_WhenExists()
        {
            var (db, service, _, _, _, _) = BuildTestContextAndService();
            var (customer, plan) = await SeedBasicData(db);
            var policy = new PolicyAssignment { Status = PolicyStatus.Active, CustomerId = customer.Id, PlanId = plan.Id };
            db.PolicyAssignments.Add(policy);
            await db.SaveChangesAsync();

            var result = await service.GetPolicyByIdAsync(policy.Id);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetPolicyByIdAsync_ShouldThrowNotFoundException_WhenPolicyDoesNotExist()
        {
            var (_, service, _, _, _, _) = BuildTestContextAndService();

            await Assert.ThrowsAsync<NotFoundException>(() => service.GetPolicyByIdAsync(999));
        }

        [Fact]
        public async Task GetPolicyByIdAsync_ShouldMapEntityToDtoCorrectly()
        {
            var (db, service, _, _, _, _) = BuildTestContextAndService();
            var (customer, plan) = await SeedBasicData(db);
            var policy = new PolicyAssignment { CustomerId = customer.Id, PlanId = plan.Id, TermYears = 10, Status = PolicyStatus.Active, PolicyNumber = "POL123" };
            db.PolicyAssignments.Add(policy);
            await db.SaveChangesAsync();

            var result = await service.GetPolicyByIdAsync(policy.Id);

            Assert.Equal("POL123", result.PolicyNumber);
            Assert.Equal(10, result.TermYears);
            Assert.Equal("Active", result.Status);
        }

        // --- 3. GetAllPoliciesAsync (3 Tests) ---

        [Fact]
        public async Task GetAllPoliciesAsync_ShouldReturnAllPolicies_WhenDataExists()
        {
            var (db, service, _, _, _, _) = BuildTestContextAndService();
            var (customer, plan) = await SeedBasicData(db);
            db.PolicyAssignments.AddRange(
                new PolicyAssignment { CustomerId = customer.Id, PlanId = plan.Id },
                new PolicyAssignment { CustomerId = customer.Id, PlanId = plan.Id });
            await db.SaveChangesAsync();

            var results = await service.GetAllPoliciesAsync();

            Assert.Equal(2, results.Count());
        }

        [Fact]
        public async Task GetAllPoliciesAsync_ShouldReturnEmptyList_WhenNoDataExists()
        {
            var (_, service, _, _, _, _) = BuildTestContextAndService();

            var results = await service.GetAllPoliciesAsync();

            Assert.Empty(results);
        }

        [Fact]
        public async Task GetAllPoliciesAsync_ShouldMapEntityToDtoCorrectly()
        {
            var (db, service, _, _, _, _) = BuildTestContextAndService();
            var (customer, plan) = await SeedBasicData(db);
            db.PolicyAssignments.Add(new PolicyAssignment { PolicyNumber = "123", CustomerId = customer.Id, PlanId = plan.Id });
            await db.SaveChangesAsync();

            var results = await service.GetAllPoliciesAsync();

            Assert.Equal("123", results.First().PolicyNumber);
        }

       
    }
}
