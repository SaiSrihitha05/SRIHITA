using System;

namespace Application.DTOs
{
    public class AgentCommissionBreakdownDto
    {
        public string PolicyNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public decimal CoverageAmount { get; set; }
        public decimal PremiumPaid { get; set; }
        public decimal CommissionEarned { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AgentEarningsSummaryDto
    {
        public int TotalPoliciesSold { get; set; }
        public decimal TotalCommissionEarned { get; set; }
        public decimal PendingCommission { get; set; }
        public List<AgentCommissionBreakdownDto> CommissionBreakdown { get; set; } = new();
    }
}
