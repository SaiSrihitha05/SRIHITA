using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class CommissionLogicTests
    {
        private (InsuranceDbContext db, PaymentService paymentService, DashboardService dashboardService) BuildTestContext()
        {
            var dbOptions = new DbContextOptionsBuilder<InsuranceDbContext>()
                .UseInMemoryDatabase($"CommissionTestDb_{Guid.NewGuid()}")
                .Options;

            var dbContext = new InsuranceDbContext(dbOptions);

            var paymentRepo = new PaymentRepository(dbContext);
            var policyRepo = new PolicyRepository(dbContext);
            var userRepo = new UserRepository(dbContext);
            var planRepo = new PlanRepository(dbContext);
            var claimRepo = new ClaimRepository(dbContext);
            var notifyRepo = new NotificationRepository(dbContext);

            var mockNotify = new Mock<INotificationService>();
            var mockEmail = new Mock<IEmailService>();

            var paymentService = new PaymentService(
                paymentRepo,
                userRepo,
                policyRepo,
                mockNotify.Object,
                mockEmail.Object);

            var dashboardService = new DashboardService(
                userRepo,
                planRepo,
                policyRepo,
                paymentRepo,
                claimRepo,
                notifyRepo);

            return (dbContext, paymentService, dashboardService);
        }

        [Fact]
        public async Task Commission_ShouldStayPending_UntilFirstPayment()
        {
            // Arrange
            var (db, paymentService, dashboardService) = BuildTestContext();

            var agent = new User { Id = 2, Name = "Agent", Role = UserRole.Agent, IsActive = true };
            var customer = new User { Id = 3, Name = "Customer", Role = UserRole.Customer, IsActive = true };
            var plan = new Plan { Id = 1, PlanName = "Gold Plan", CommissionRate = 10, IsActive = true };

            db.Users.AddRange(agent, customer);
            db.Plans.Add(plan);

            var policy = new PolicyAssignment
            {
                Id = 1,
                PolicyNumber = "POL001",
                AgentId = agent.Id,
                CustomerId = customer.Id,
                PlanId = plan.Id,
                TotalPremiumAmount = 1000,
                Status = PolicyStatus.Active,
                CommissionStatus = CommissionStatus.Pending,
                StartDate = DateTime.UtcNow.AddDays(1),
                NextDueDate = DateTime.UtcNow.AddDays(1)
            };
            db.PolicyAssignments.Add(policy);
            await db.SaveChangesAsync();

            // Act - Get dashboard before payment
            var dashboardBefore = await dashboardService.GetAgentDashboard(agent.Id);

            // Assert - Commission should be 0
            Assert.Equal(0, dashboardBefore.TotalCommissionEarned);
            Assert.Equal(CommissionStatus.Pending, policy.CommissionStatus);

            // Act - Make first payment
            var paymentDto = new CreatePaymentDto
            {
                PolicyAssignmentId = policy.Id,
                PaymentMethod = "CreditCard",
                ExtraInstallments = 0
            };
            await paymentService.MakePaymentAsync(customer.Id, paymentDto);

            // Assert - Policy CommissionStatus should be Paid
            var updatedPolicy = await db.PolicyAssignments.FindAsync(policy.Id);
            Assert.Equal(CommissionStatus.Paid, updatedPolicy.CommissionStatus);

            // Act - Get dashboard after payment
            var dashboardAfter = await dashboardService.GetAgentDashboard(agent.Id);

            // Assert - Commission should now be calculated (10% of 1000 = 100)
            Assert.Equal(100, dashboardAfter.TotalCommissionEarned);
        }
    }
}
