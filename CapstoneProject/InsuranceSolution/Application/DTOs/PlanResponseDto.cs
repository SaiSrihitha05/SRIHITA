using Domain.Enums;

namespace Application.DTOs
{
    public class PlanResponseDto
    {
        public int Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public PlanCategory PlanType { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal BaseRate { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public decimal MinCoverageAmount { get; set; }
        public decimal MaxCoverageAmount { get; set; }
        public int? MinTermYears { get; set; }
        public int? MaxTermYears { get; set; }
        public int MinNominees { get; set; }
        public int MaxNominees { get; set; }
        public int GracePeriodDays { get; set; }
        public bool HasMaturityBenefit { get; set; }
        public bool IsReturnOfPremium { get; set; }
        public int MaxPolicyMembersAllowed { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public decimal CommissionRate { get; set; }
        public bool HasDeathBenefit { get; set; }
        public bool HasBonus { get; set; }
        public bool HasLoanFacility { get; set; }
        public bool CoverageIncreasing { get; set; }
        public decimal CoverageIncreaseRate { get; set; }
        public bool IsCoverageUntilAge { get; set; }
        public int? CoverageUntilAge { get; set; }
        public decimal BonusRate { get; set; }
        public decimal TerminalBonusRate { get; set; }
        public int LoanEligibleAfterYears { get; set; }
        public decimal MaxLoanPercentage { get; set; }
        public decimal LoanInterestRate { get; set; }
        public decimal ReinstatementPenaltyAmount { get; set; }
        public int ReinstatementDays { get; set; }
    }
}