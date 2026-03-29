using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class PolicyResponseDto
    {
        public int Id { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? AgentId { get; set; }
        public string? AgentName { get; set; }
        public string? AgentEmail { get; set; }
        public string? AgentPhone { get; set; }
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public int CustomerAge { get; set; }
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TermYears { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string CommissionStatus { get; set; } = string.Empty;
        public decimal TotalPremiumAmount { get; set; }
        public string PremiumFrequency { get; set; } = string.Empty;
        public DateTime NextDueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool PlanHasLoanFacility { get; set; }
        public int PlanLoanEligibleAfterYears { get; set; }
        public decimal PlanMaxLoanPercentage { get; set; }
        public bool PlanCoverageIncreasing { get; set; }
        public bool PlanHasBonus { get; set; }
        public bool PlanIsCoverageUntilAge { get; set; }
        public int PlanCoverageUntilAge { get; set; }
        public decimal PlanCoverageIncreaseRate { get; set; }
        public int PlanMinAge { get; set; }
        public int PlanMaxAge { get; set; }
        public decimal PlanMinCoverageAmount { get; set; }
        public decimal PlanMaxCoverageAmount { get; set; }
        public int PlanMinNominees { get; set; }
        public int PlanMaxNominees { get; set; }
        public int PlanMaxMembers { get; set; }
        public decimal PlanBonusRate { get; set; }
        public decimal PlanTerminalBonusRate { get; set; }
        public string Address { get; set; } = string.Empty;
        public bool CanRenew { get; set; }
        public bool? HasPaidPremiums { get; set; }
        public int? PlanGracePeriodDays { get; set; }
        public BonusCalculationResult BonusDetails { get; set; } = new();
        public List<PolicyMemberResponseDto> Members { get; set; } = new();
        public List<PolicyNomineeResponseDto> Nominees { get; set; } = new();
        public List<DocumentResponseDto> Documents { get; set; } = new();
    }
}