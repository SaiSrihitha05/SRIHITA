using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Services;
using Application.Tests.Common;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Services
{
    public class DashboardServiceTests : ApplicationTestBase
    {
        private readonly DashboardService _dashboardService;
        private readonly IUserRepository _userRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly INotificationRepository _notificationRepository;

        public DashboardServiceTests()
        {
            _userRepository = new UserRepository(Context);
            _planRepository = new PlanRepository(Context);
            _policyRepository = new PolicyRepository(Context);
            _paymentRepository = new PaymentRepository(Context);
            _claimRepository = new ClaimRepository(Context);
            _notificationRepository = new NotificationRepository(Context);

            _dashboardService = new DashboardService(
                _userRepository,
                _planRepository,
                _policyRepository,
                _paymentRepository,
                _claimRepository,
                _notificationRepository);
        }

        #region GetAdminDashboardAsync Tests (5)
        [Fact]
        public async Task GetAdminDashboardAsync_EmptyDb_ReturnsZeroStats()
        {
            var res = await _dashboardService.GetAdminDashboardAsync();
            Assert.Equal(0, res.TotalCustomers);
            Assert.Equal(0, res.TotalPremiumCollected);
        }
        [Fact]
        public async Task GetAdminDashboardAsync_WithData_ReturnsCorrectCounts()
        {
            await Context.Users.AddAsync(new User { Role = UserRole.Customer, Email = "c1@t.com" });
            await Context.Users.AddAsync(new User { Role = UserRole.Agent, Email = "a1@t.com" });
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { Status = PolicyStatus.Active });
            await Context.Payments.AddAsync(new Payment { Amount = 1000, PaymentDate = DateTime.UtcNow });
            await Context.SaveChangesAsync();

            var res = await _dashboardService.GetAdminDashboardAsync();
            Assert.Equal(1, res.TotalCustomers);
            Assert.Equal(1, res.TotalAgents);
            Assert.Equal(1000, res.TotalPremiumCollected);
        }
        [Fact]
        public async Task GetAdminDashboardAsync_RecentItems_ReturnsMax5()
        {
            for (int i = 0; i < 10; i++) await Context.PolicyAssignments.AddAsync(new PolicyAssignment { PolicyNumber = $"P{i}", CreatedAt = DateTime.UtcNow });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetAdminDashboardAsync();
            Assert.Equal(5, res.RecentPolicies.Count);
        }
        [Fact]
        public async Task GetAdminDashboardAsync_MonthlyRevenue_GroupsCorrectly()
        {
            var date = new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            await Context.Payments.AddAsync(new Payment { Amount = 500, PaymentDate = date });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetAdminDashboardAsync();
            var jan = res.MonthlyRevenue.FirstOrDefault(m => m.Year == 2023 && m.Month == 1);
            Assert.NotNull(jan);
            Assert.Equal(500, jan.Revenue);
        }
        [Fact]
        public async Task GetAdminDashboardAsync_ClaimStats_AggregatesCorrectly()
        {
            await Context.Claims.AddAsync(new InsuranceClaim { Status = ClaimStatus.Settled, SettlementAmount = 100 });
            await Context.Claims.AddAsync(new InsuranceClaim { Status = ClaimStatus.Rejected });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetAdminDashboardAsync();
            Assert.Equal(2, res.TotalClaims);
            Assert.Equal(100, res.TotalSettlementAmount);
            Assert.Equal(50, res.ClaimApprovalRate);
        }
        #endregion

        #region GetCustomerDashboardAsync Tests (5)
        [Fact]
        public async Task GetCustomerDashboardAsync_ValidId_ReturnsMyStats()
        {
            var customer = new User { Role = UserRole.Customer, Email = "c@t.com" };
            await Context.Users.AddAsync(customer); await Context.SaveChangesAsync();
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { CustomerId = customer.Id, Status = PolicyStatus.Active });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetCustomerDashboardAsync(customer.Id);
            Assert.Equal(1, res.TotalPolicies);
        }
        [Fact]
        public async Task GetCustomerDashboardAsync_InvalidId_ReturnsEmptyStats()
        {
            var res = await _dashboardService.GetCustomerDashboardAsync(999);
            Assert.Equal(0, res.TotalPolicies);
        }
        [Fact]
        public async Task GetCustomerDashboardAsync_DueDateCheck_ReturnsSoonest()
        {
            var cid = 1;
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { CustomerId = cid, NextDueDate = DateTime.UtcNow.AddDays(5), Status = PolicyStatus.Active });
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { CustomerId = cid, NextDueDate = DateTime.UtcNow.AddDays(15), Status = PolicyStatus.Active });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetCustomerDashboardAsync(cid);
            Assert.Equal(DateTime.UtcNow.AddDays(5).Date, res.NextDueDate?.Date);
            Assert.True(res.IsPaymentDueSoon);
        }
        [Fact]
        public async Task GetCustomerDashboardAsync_OverdueCheck_ReturnsTrueIfLapsed()
        {
            var cid = 1;
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { CustomerId = cid, NextDueDate = DateTime.UtcNow.AddDays(-5), Status = PolicyStatus.Active });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetCustomerDashboardAsync(cid);
            Assert.True(res.HasOverduePayment);
            Assert.Equal(5, res.DaysOverdue);
        }
        [Fact]
        public async Task GetCustomerDashboardAsync_NotificationCheck_ReturnsRecent5()
        {
            for (int i = 0; i < 10; i++) await Context.Notifications.AddAsync(new Notification { UserId = 1, Title = $"T{i}", CreatedAt = DateTime.UtcNow });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetCustomerDashboardAsync(1);
            Assert.Equal(5, res.RecentNotifications.Count);
        }
        #endregion

        #region GetAgentDashboard Tests (5)
        [Fact]
        public async Task GetAgentDashboard_ValidId_ReturnsMyPerformance()
        {
            var agent = new User { Role = UserRole.Agent, Email = "a@t.com" };
            await Context.Users.AddAsync(agent); await Context.SaveChangesAsync();
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { AgentId = agent.Id });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetAgentDashboard(agent.Id);
            Assert.Equal(1, res.TotalAssignedPolicies);
        }
        [Fact]
        public async Task GetAgentDashboard_EmptySales_ReturnsZeroCommission()
        {
            var res = await _dashboardService.GetAgentDashboard(1);
            Assert.Equal(0, res.TotalCommissionEarned);
            Assert.Equal(1.0m, res.CurrentCommissionRate);
        }
        [Fact]
        public async Task GetAgentDashboard_ExceedThreshold_AppliesBonus()
        {
            var agent = new User { Role = UserRole.Agent, Email = "a@t.com" };
            await Context.Users.AddAsync(agent);
            var plan = new Plan { CommissionRate = 10 }; await Context.Plans.AddAsync(plan);
            for (int i = 0; i < 11; i++) await Context.PolicyAssignments.AddAsync(new PolicyAssignment { AgentId = agent.Id, PlanId = plan.Id, TotalPremiumAmount = 100 });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetAgentDashboard(agent.Id);
            // 11 * 100 * 0.1 * 1.5 = 11 * 10 * 1.5 = 165
            Assert.Equal(165, res.TotalCommissionEarned);
            Assert.True(res.IsBonusRateApplied);
        }
        [Fact]
        public async Task GetAgentDashboard_MonthlySold_GroupsCorrectly()
        {
            var aid = 1;
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { AgentId = aid, CreatedAt = DateTime.UtcNow });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetAgentDashboard(aid);
            Assert.NotEmpty(res.MonthlyPoliciesSold);
        }
        [Fact]
        public async Task GetAgentDashboard_CustomerCount_IsUnique()
        {
            var aid = 1;
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { AgentId = aid, CustomerId = 10 });
            await Context.PolicyAssignments.AddAsync(new PolicyAssignment { AgentId = aid, CustomerId = 10 });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetAgentDashboard(aid);
            Assert.Equal(1, res.AssignedCustomers);
        }
        #endregion

        #region GetClaimsOfficerDashboardAsync Tests (5)
        [Fact]
        public async Task GetClaimsOfficerDashboardAsync_ValidId_ReturnsMyStats()
        {
            var officer = new User { Role = UserRole.ClaimsOfficer, Email = "o@t.com" };
            await Context.Users.AddAsync(officer); await Context.SaveChangesAsync();
            await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = officer.Id, Status = ClaimStatus.UnderReview });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetClaimsOfficerDashboardAsync(officer.Id);
            Assert.Equal(1, res.TotalAssignedClaims);
            Assert.Equal(1, res.PendingReviewClaims);
        }
        [Fact]
        public async Task GetClaimsOfficerDashboardAsync_ProcessingTime_CalculatesAverage()
        {
            var oid = 1;
            var filed = DateTime.UtcNow.AddDays(-10);
            await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = oid, FiledDate = filed, ProcessedDate = filed.AddDays(4), Status = ClaimStatus.Settled });
            await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = oid, FiledDate = filed, ProcessedDate = filed.AddDays(6), Status = ClaimStatus.Rejected });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetClaimsOfficerDashboardAsync(oid);
            Assert.Equal(5.0, res.AverageProcessingTimeDays);
        }
        [Fact]
        public async Task GetClaimsOfficerDashboardAsync_UrgentClaims_FilterCorrectly()
        {
            var oid = 1;
            await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = oid, Status = ClaimStatus.UnderReview, FiledDate = DateTime.UtcNow.AddDays(-10) }); // Urgent
            await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = oid, Status = ClaimStatus.UnderReview, FiledDate = DateTime.UtcNow.AddDays(-2) });  // Not urgent
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetClaimsOfficerDashboardAsync(oid);
            Assert.Equal(1, res.UrgentClaims);
        }
        [Fact]
        public async Task GetClaimsOfficerDashboardAsync_RecentAssigned_ReturnsMax5()
        {
            for (int i = 0; i < 10; i++) await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = 1, FiledDate = DateTime.UtcNow });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetClaimsOfficerDashboardAsync(1);
            Assert.Equal(5, res.RecentAssignedClaims.Count);
        }
        [Fact]
        public async Task GetClaimsOfficerDashboardAsync_MonthlyProcessed_GroupsCorrectly()
        {
            var oid = 1;
            var now = DateTime.UtcNow;
            await Context.Claims.AddAsync(new InsuranceClaim { ClaimsOfficerId = oid, ProcessedDate = now, Status = ClaimStatus.Settled });
            await Context.SaveChangesAsync();
            var res = await _dashboardService.GetClaimsOfficerDashboardAsync(oid);
            Assert.NotEmpty(res.MonthlyProcessed);
        }
        #endregion
    }
}
