using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Application/DTOs/AdminDashboardDto.cs
namespace Application.DTOs
{
    public class AdminDashboardDto
    {
        // Users
        public int TotalCustomers { get; set; }
        public int TotalAgents { get; set; }
        public int TotalClaimsOfficers { get; set; }

        // Plans
        public int TotalActivePlans { get; set; }
        public int TotalInactivePlans { get; set; }

        // Policies
        public int TotalPolicies { get; set; }
        public int PendingPolicies { get; set; }
        public int ActivePolicies { get; set; }
        public int ExpiredPolicies { get; set; }
        public int RejectedPolicies { get; set; }
        public int MaturedPolicies { get; set; }
        public int ClosedPolicies { get; set; }

        // Payments
        public decimal TotalPremiumCollected { get; set; }
        public int TotalPaymentsCount { get; set; }
        public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();

        // Claims
        public int TotalClaims { get; set; }
        public int SubmittedClaims { get; set; }
        public int UnderReviewClaims { get; set; }
        public int SettledClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal TotalSettlementAmount { get; set; }
        public decimal ClaimApprovalRate { get; set; }

        // Recent Activity
        public List<PolicyResponseDto> RecentPolicies { get; set; } = new();
        public List<ClaimResponseDto> RecentClaims { get; set; } = new();
        public List<AgentPerformanceDto> AgentPerformance { get; set; } = new();
        public List<MonthlyPolicyGrowthDto> MonthlyPolicyGrowth { get; set; } = new();
        public List<RevenueByPlanDto> RevenueByPlanType { get; set; } = new();
    }
}
