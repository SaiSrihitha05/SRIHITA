using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.DTOs
{
    public class AgentDashboardDto
    {
        public int TotalAssignedPolicies { get; set; }
        public int PendingPolicies { get; set; }
        public int ActivePolicies { get; set; }
        public int RejectedPolicies { get; set; }
        public List<PolicyResponseDto> RecentAssignedPolicies { get; set; } = new();
        public int AssignedCustomers { get; set; }       // ← missing
        public decimal TotalCommissionEarned { get; set; } // ← missing
        public decimal CurrentCommissionRate { get; set; }
        public bool IsBonusRateApplied { get; set; }
        public List<MonthlyPolicySoldDto> MonthlyPoliciesSold { get; set; } = new();
        public List<PolicyByPlanTypeDto> PoliciesByPlanType { get; set; } = new();
        public List<MonthlyCommissionDto> MonthlyCommission { get; set; } = new();
    }
}