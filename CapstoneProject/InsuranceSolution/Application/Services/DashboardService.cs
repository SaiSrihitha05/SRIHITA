using Application.DTOs;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPlanRepository _planRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IClaimRepository _claimRepository;
        private readonly INotificationRepository _notificationRepository;


        public DashboardService(
            IUserRepository userRepository,
            IPlanRepository planRepository,
            IPolicyRepository policyRepository,
            IPaymentRepository paymentRepository,
            IClaimRepository claimRepository,
            INotificationRepository notificationRepository)
        {
            _userRepository = userRepository;
            _planRepository = planRepository;
            _policyRepository = policyRepository;
            _paymentRepository = paymentRepository;
            _claimRepository = claimRepository;
            _notificationRepository = notificationRepository;
        }

        public async Task<AdminDashboardDto> GetAdminDashboardAsync()
        {
            // Users
            var customers = await _userRepository
                .GetByRoleAsync(UserRole.Customer);
            var agents = await _userRepository
                .GetByRoleAsync(UserRole.Agent);
            var claimsOfficers = await _userRepository
                .GetByRoleAsync(UserRole.ClaimsOfficer);

            // Plans
            var allPlans = await _planRepository.GetAllAsync();

            // Policies
            var allPolicies = await _policyRepository.GetAllAsync();

            // Payments
            var allPayments = await _paymentRepository.GetAllAsync();

            // Claims
            var allClaims = await _claimRepository.GetAllAsync();

            // Monthly revenue — last 12 months
            var monthlyRevenue = allPayments
                .Where(p => p.PaymentDate >= DateTime.UtcNow.AddMonths(-12))
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new MonthlyRevenueDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1)
                                        .ToString("MMM yyyy"),
                    Revenue = g.Sum(p => p.Amount),
                    PaymentsCount = g.Count()
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            // Recent 5 policies and claims
            var recentPolicies = allPolicies
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToList();

            var recentClaims = allClaims
                .OrderByDescending(c => c.FiledDate)
                .Take(5)
                .ToList();

            // Agent Performance 
            var agentPerformance = GetAgentPerformanceAsync(
                allPolicies.ToList(), agents.ToList());

            // Monthly Policy Growth 
            var monthlyPolicyGrowth = allPolicies
                .Where(p => p.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
                .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                .Select(g => new MonthlyPolicyGrowthDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1)
                                           .ToString("MMM yyyy"),
                    PoliciesCreated = g.Count(),
                    ActivePolicies = g.Count(p => p.Status == PolicyStatus.Active)
                })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            // Revenue by Plan Type 
            var revenueByPlan = allPayments
                .GroupBy(p => new
                {
                    PlanName = p.PolicyAssignment?.Plan?.PlanName ?? "Unknown",
                    PlanType = p.PolicyAssignment?.Plan?.PlanType.ToString() ?? "Unknown"
                })
                .Select(g => new RevenueByPlanDto
                {
                    PlanName = g.Key.PlanName,
                    PlanType = g.Key.PlanType,
                    TotalPolicies = g.Select(p => p.PolicyAssignmentId).Distinct().Count(),
                    TotalRevenue = g.Sum(p => p.Amount)
                })
                .OrderByDescending(r => r.TotalRevenue)
                .ToList();

            // Claim Approval Rate 
            var settledCount = allClaims.Count(c => c.Status == ClaimStatus.Settled);
            var rejectedCount = allClaims.Count(c => c.Status == ClaimStatus.Rejected);
            var approvalRate = (settledCount + rejectedCount) > 0
                ? Math.Round((decimal)settledCount /
                  (settledCount + rejectedCount) * 100, 2)
                : 0;

            return new AdminDashboardDto
            {
                // Users
                TotalCustomers = customers.Count(),
                TotalAgents = agents.Count(),
                TotalClaimsOfficers = claimsOfficers.Count(),

                // Plans
                TotalActivePlans = allPlans.Count(p => p.IsActive),
                TotalInactivePlans = allPlans.Count(p => !p.IsActive),

                // Policies
                TotalPolicies = allPolicies.Count(),
                PendingPolicies = allPolicies
                    .Count(p => p.Status == PolicyStatus.Pending),
                ActivePolicies = allPolicies
                    .Count(p => p.Status == PolicyStatus.Active),
                ExpiredPolicies = allPolicies
                    .Count(p => p.Status == PolicyStatus.Expired),
                RejectedPolicies = allPolicies
                    .Count(p => p.Status == PolicyStatus.Rejected),
                MaturedPolicies = allPolicies
                    .Count(p => p.Status == PolicyStatus.Matured),
                ClosedPolicies = allPolicies
                    .Count(p => p.Status == PolicyStatus.Closed),

                // Payments
                TotalPremiumCollected = allPayments.Sum(p => p.Amount),
                TotalPaymentsCount = allPayments.Count(),
                MonthlyRevenue = monthlyRevenue,

                // Claims
                TotalClaims = allClaims.Count(),
                SubmittedClaims = allClaims
                    .Count(c => c.Status == ClaimStatus.Submitted),
                UnderReviewClaims = allClaims
                    .Count(c => c.Status == ClaimStatus.UnderReview),
                SettledClaims = allClaims
                    .Count(c => c.Status == ClaimStatus.Settled),
                RejectedClaims = allClaims
                    .Count(c => c.Status == ClaimStatus.Rejected),
                TotalSettlementAmount = allClaims
                    .Where(c => c.SettlementAmount.HasValue)
                    .Sum(c => c.SettlementAmount!.Value),

                // Recent
                RecentPolicies = recentPolicies.Select(MapPolicyToDto).ToList(),
                RecentClaims = recentClaims.Select(MapClaimToDto).ToList(),
                ClaimApprovalRate = approvalRate,
                AgentPerformance = agentPerformance,
                MonthlyPolicyGrowth = monthlyPolicyGrowth,
                RevenueByPlanType = revenueByPlan,
            };
        }

        private static List<AgentPerformanceDto> GetAgentPerformanceAsync(
    List<PolicyAssignment> allPolicies,
    List<User> agents)
        {
            var performance = agents.Select(agent =>
            {
                var agentPolicies = allPolicies
                    .Where(p => p.AgentId == agent.Id).ToList();

                var paidPolicies = agentPolicies
                    .Where(p => p.CommissionStatus == CommissionStatus.Paid).ToList();

                var totalPremium = paidPolicies.Sum(p => p.TotalPremiumAmount);

                // Commission = sum of (premium * plan commission rate)
                var commission = paidPolicies.Sum(p =>
                    p.TotalPremiumAmount * (p.Plan?.CommissionRate ?? 0) / 100);

                return new AgentPerformanceDto
                {
                    AgentId = agent.Id,
                    AgentName = agent.Name,
                    PoliciesSold = agentPolicies.Count,
                    ActivePolicies = agentPolicies
                        .Count(p => p.Status == PolicyStatus.Active),
                    TotalPremiumGenerated = totalPremium,
                    CommissionEarned = Math.Round(commission, 2)
                };
            })
            .OrderByDescending(a => a.PoliciesSold)
            .ToList();

            // Assign rank
            for (int i = 0; i < performance.Count; i++)
                performance[i].Rank = i + 1;

            return performance;
        }

        public async Task<CustomerDashboardDto> GetCustomerDashboardAsync(
            int customerId)
        {
            var myPolicies = await _policyRepository
                .GetByCustomerIdAsync(customerId);
            var myPayments = await _paymentRepository
                .GetByCustomerIdAsync(customerId);
            var myClaims = await _claimRepository
                .GetByCustomerIdAsync(customerId);

            // Find nearest due date across all active policies
            var activePolicies = myPolicies
                .Where(p => p.Status == PolicyStatus.Active)
                .ToList();

            var nextDuePolicy = activePolicies
                .OrderBy(p => p.NextDueDate)
                .FirstOrDefault();

            var isDueSoon = nextDuePolicy != null &&
                            nextDuePolicy.NextDueDate.Date <=
                            DateTime.UtcNow.Date.AddDays(30);

            var recentPayments = myPayments
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToList();

            // Overdue check
            var hasOverdue = nextDuePolicy != null &&
                                DateTime.UtcNow.Date > nextDuePolicy.NextDueDate.Date;
            var daysOverdue = hasOverdue
                ? (int)(DateTime.UtcNow.Date - nextDuePolicy!.NextDueDate.Date).TotalDays
                : 0;

            // Notifications
            var notifications = await _notificationRepository
                .GetByUserIdAsync(customerId);
            var recentNotifications = notifications
                .Take(5)
                .Select(n => new NotificationResponseDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type.ToString(),
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    PolicyId = n.PolicyAssignmentId,
                    ClaimId = n.ClaimId,
                    PaymentId = n.PaymentId
                }).ToList();

            return new CustomerDashboardDto
            {
                TotalPolicies = myPolicies.Count(),
                ActivePolicies = myPolicies
                    .Count(p => p.Status == PolicyStatus.Active),
                PendingPolicies = myPolicies
                    .Count(p => p.Status == PolicyStatus.Pending),
                TotalPremiumPaid = myPayments.Sum(p => p.Amount),
                NextDueDate = nextDuePolicy?.NextDueDate,
                NextDueAmount = nextDuePolicy?.TotalPremiumAmount,
                NextDuePolicyId = nextDuePolicy?.Id,
                IsPaymentDueSoon = isDueSoon,
                TotalClaims = myClaims.Count(),
                PendingClaims = myClaims
                    .Count(c => c.Status == ClaimStatus.Submitted ||
                                c.Status == ClaimStatus.UnderReview),
                RecentPayments = recentPayments
                    .Select(p => MapPaymentToDto(p)).ToList(),
                MyPolicies = myPolicies
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(MapPolicyToDto).ToList(),
                HasOverduePayment = hasOverdue,
                DaysOverdue = daysOverdue,
                RecentNotifications = recentNotifications,
                MyClaimsByStatus = myClaims
                    .GroupBy(c => c.Status.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<AgentDashboardDto> GetAgentDashboard(int agentId)
        {
            var myPolicies = await _policyRepository.GetByAgentIdAsync(agentId);
            var policiesList = myPolicies.ToList();

            var paidPolicies = policiesList
                .Where(p => p.CommissionStatus == CommissionStatus.Paid).ToList();

            // Commission with threshold logic
            var baseCommission = paidPolicies.Sum(p =>
                p.TotalPremiumAmount * (p.Plan?.CommissionRate ?? 0) / 100);

            var threshold = 10;
            var bonusMultiplier = policiesList.Count > threshold ? 1.5m : 1.0m;
            var totalCommission = Math.Round(baseCommission * bonusMultiplier, 2);
            var isBonusApplied = policiesList.Count > threshold;
            var commissionRate = isBonusApplied ? 1.5m : 1.0m;

            // Monthly policies sold
            var monthlyPolicies = policiesList
                .Where(p => p.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
                .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                .Select(g => new MonthlyPolicySoldDto
                {
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1)
                                         .ToString("MMM yyyy"),
                    PoliciesSold = g.Count(),
                    CommissionEarned = Math.Round(g
                        .Where(p => p.CommissionStatus == CommissionStatus.Paid)
                        .Sum(p =>
                        p.TotalPremiumAmount *
                        (p.Plan?.CommissionRate ?? 0) / 100 * bonusMultiplier), 2)
                })
                .OrderBy(m => m.MonthName)
                .ToList();

            // Policies by plan type
            var byPlanType = policiesList
                .GroupBy(p => p.Plan?.PlanType.ToString() ?? "Unknown")
                .Select(g => new PolicyByPlanTypeDto
                {
                    PlanType = g.Key,
                    Count = g.Count()
                }).ToList();

            // Unique customers
            var assignedCustomers = policiesList
                .Select(p => p.CustomerId)
                .Distinct()
                .Count();

            var monthlyCommission = policiesList
                .Where(p => p.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
                .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                .Select(g =>
                {
                    var monthPoliciesCount = g.Count();
                    var isBonus = monthPoliciesCount > threshold;
                    var multiplier = isBonus ? 1.5m : 1.0m;
                    var earned = Math.Round(g
                        .Where(p => p.CommissionStatus == CommissionStatus.Paid)
                        .Sum(p =>
                        p.TotalPremiumAmount *
                        (p.Plan?.CommissionRate ?? 0) / 100) * multiplier, 2);

                    return new MonthlyCommissionDto
                    {
                        MonthName = new DateTime(g.Key.Year, g.Key.Month, 1)
                                               .ToString("MMM yyyy"),
                        PoliciesSold = monthPoliciesCount,
                        CommissionEarned = earned,
                        BonusApplied = isBonus
                    };
                })
                .OrderBy(m => m.MonthName)
                .ToList();

            return new AgentDashboardDto
            {
                TotalAssignedPolicies = policiesList.Count,
                AssignedCustomers = assignedCustomers,
                PendingPolicies = policiesList
                    .Count(p => p.Status == PolicyStatus.Pending),
                ActivePolicies = policiesList
                    .Count(p => p.Status == PolicyStatus.Active),
                RejectedPolicies = policiesList
                    .Count(p => p.Status == PolicyStatus.Rejected),
                TotalCommissionEarned = totalCommission,
                CurrentCommissionRate = commissionRate,
                IsBonusRateApplied = isBonusApplied,
                MonthlyPoliciesSold = monthlyPolicies,
                PoliciesByPlanType = byPlanType,
                TotalCoverageAmountSold = policiesList.SelectMany(p => p.PolicyMembers).Sum(m => m.CoverageAmount),
                RecentAssignedPolicies = policiesList
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .Select(MapPolicyToDto).ToList(),
                MonthlyCommission = monthlyCommission,
            };
        }

        public async Task<AgentEarningsSummaryDto> GetAgentEarningsAsync(int agentId)
        {
            var myPolicies = await _policyRepository.GetByAgentIdAsync(agentId);
            var myPayments = await _paymentRepository.GetAllAsync(); // Join locally or via repo if efficient

            var agentPolicyIds = myPolicies.Select(p => p.Id).ToList();
            
            // Filter payments for this agent's policies
            var relevantPayments = myPayments
                .Where(p => agentPolicyIds.Contains(p.PolicyAssignmentId) && p.Status == PaymentStatus.Completed)
                .ToList();

            var breakdown = relevantPayments.Select(p =>
            {
                var policy = myPolicies.First(poly => poly.Id == p.PolicyAssignmentId);
                var rate = policy.Plan?.CommissionRate ?? 20m; // Default to 20% if not set
                var earned = p.Amount * rate / 100;

                return new AgentCommissionBreakdownDto
                {
                    PolicyNumber = policy.PolicyNumber,
                    CustomerName = policy.Customer?.Name ?? "Unknown",
                    PlanName = policy.Plan?.PlanName ?? "Unknown",
                    CoverageAmount = policy.PolicyMembers.Sum(m => m.CoverageAmount),
                    PremiumPaid = p.Amount,
                    CommissionEarned = Math.Round(earned, 2),
                    PaymentDate = p.PaymentDate,
                    Status = policy.Status.ToString()
                };
            })
            .OrderByDescending(b => b.PaymentDate)
            .ToList();

            var totalEarned = breakdown.Sum(b => b.CommissionEarned);
            
            // Pending commission: Policies that are active but haven't had commission "Processed" if we had that flag.
            // For now, let's say pending is anything on a policy where CommissionStatus is Pending.
            var pendingPolicies = myPolicies.Where(p => p.CommissionStatus == CommissionStatus.Pending).ToList();
            var pendingAmt = pendingPolicies.Sum(p => p.TotalPremiumAmount * (p.Plan?.CommissionRate ?? 20m) / 100);

            return new AgentEarningsSummaryDto
            {
                TotalPoliciesSold = myPolicies.Count(),
                TotalCommissionEarned = totalEarned,
                PendingCommission = Math.Round(pendingAmt, 2),
                CommissionBreakdown = breakdown
            };
        }

        public async Task<ClaimsOfficerDashboardDto> GetClaimsOfficerDashboardAsync(
            int officerId)
        {
            var myClaims = await _claimRepository
                .GetByClaimsOfficerIdAsync(officerId);
            var claimsList = myClaims.ToList();

            // Average processing time
            var processedClaims = claimsList
                .Where(c => c.ProcessedDate.HasValue).ToList();



            // Urgent claims — under review for more than 7 days
            var urgentClaims = claimsList
                .Count(c => c.Status == ClaimStatus.UnderReview &&
                            (DateTime.UtcNow - c.FiledDate).TotalDays > 7);

            // Monthly processed
            var monthlyProcessed = claimsList
                .Where(c => c.ProcessedDate.HasValue &&
                            c.ProcessedDate >= DateTime.UtcNow.AddMonths(-12))
                .GroupBy(c => new
                {
                    c.ProcessedDate!.Value.Year,
                    c.ProcessedDate.Value.Month
                })
                .Select(g => new MonthlyClaimsProcessedDto
                {
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1)
                                          .ToString("MMM yyyy"),
                    ClaimsProcessed = g.Count(),
                    Approved = g.Count(c => c.Status == ClaimStatus.Settled),
                    Rejected = g.Count(c => c.Status == ClaimStatus.Rejected)
                })
                .OrderBy(m => m.MonthName)
                .ToList();

            return new ClaimsOfficerDashboardDto
            {
                TotalAssignedClaims = claimsList.Count,
                PendingReviewClaims = claimsList
                    .Count(c => c.Status == ClaimStatus.UnderReview),
                SettledClaims = claimsList
                    .Count(c => c.Status == ClaimStatus.Settled),
                RejectedClaims = claimsList
                    .Count(c => c.Status == ClaimStatus.Rejected),
                ActiveClaimsCount = claimsList
                    .Count(c => c.Status == ClaimStatus.UnderReview ||
                                c.Status == ClaimStatus.Submitted),
                UrgentClaims = urgentClaims,
                MonthlyProcessed = monthlyProcessed,
                RecentAssignedClaims = claimsList
                    .OrderByDescending(c => c.FiledDate)
                    .Take(5)
                    .Select(MapClaimToDto).ToList()
            };
        }

        // Private Mappers 
        private static PolicyResponseDto MapPolicyToDto(PolicyAssignment p) => new()
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
            EndDate = p.EndDate,
            TermYears = p.TermYears,
            Status = p.Status.ToString(),
            CommissionStatus = p.CommissionStatus.ToString(),
            TotalPremiumAmount = p.TotalPremiumAmount,
            PremiumFrequency = p.PremiumFrequency.ToString(),
            NextDueDate = p.NextDueDate,
            CreatedAt = p.CreatedAt
        };

        private static ClaimResponseDto MapClaimToDto(InsuranceClaim c) => new()
        {
            Id = c.Id,
            PolicyAssignmentId = c.PolicyAssignmentId,
            PolicyNumber = c.PolicyAssignment?.PolicyNumber ?? string.Empty,
            ClaimForMemberId = c.ClaimForMemberId,
            MemberRelationship = c.ClaimMember?.RelationshipToCustomer ?? string.Empty,
            ClaimsOfficerId = c.ClaimsOfficerId,
            ClaimsOfficerName = c.ClaimsOfficer?.Name,
            ClaimType = c.ClaimType.ToString(),
            ClaimAmount = c.ClaimAmount,
            DeathCertificateNumber = c.DeathCertificateNumber,
            FiledDate = c.FiledDate,
            Status = c.Status.ToString(),
            Remarks = c.Remarks,
            SettlementAmount = c.SettlementAmount,
            ProcessedDate = c.ProcessedDate,
            CreatedAt = c.CreatedAt
        };

        private static PaymentResponseDto MapPaymentToDto(Payment p) => new()
        {
            Id = p.Id,
            PolicyAssignmentId = p.PolicyAssignmentId,
            PolicyNumber = p.PolicyAssignment?.PolicyNumber ?? string.Empty,
            Amount = p.Amount,
            InstallmentsPaid = p.InstallmentsPaid,
            PaymentDate = p.PaymentDate,
            PaymentMethod = p.PaymentMethod,
            TransactionReference = p.TransactionReference,
            Status = p.Status.ToString(),
            InvoiceNumber = p.InvoiceNumber,
            CreatedAt = p.CreatedAt
        };
    }
}
