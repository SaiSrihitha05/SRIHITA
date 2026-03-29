using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs
{
    public class UpdatePlanDto
    {
        [Required]
        public string PlanName { get; set; } = string.Empty;

        [Required]
        public PlanCategory PlanType { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal BaseRate { get; set; }

        [Range(0, 150)]
        public int MinAge { get; set; }

        [Range(0, 150)]
        public int MaxAge { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal MinCoverageAmount { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal MaxCoverageAmount { get; set; }

        [Range(1, 50)]
        public int? MinTermYears { get; set; }

        [Range(1, 50)]
        public int? MaxTermYears { get; set; }
        [Range(1, 10)]
        public int MinNominees { get; set; } = 1;

        [Range(1, 10)]
        public int MaxNominees { get; set; } = 5;

        [Range(0, 365)]
        public int GracePeriodDays { get; set; }

        public bool HasMaturityBenefit { get; set; }
        public bool IsReturnOfPremium { get; set; }

        [Range(1, 20)]
        public int MaxPolicyMembersAllowed { get; set; }

        public bool IsActive { get; set; }
        [Range(0, 100, ErrorMessage = "Commission rate must be between 0 and 100")]
        public decimal CommissionRate { get; set; }

        public bool HasDeathBenefit { get; set; }
        public bool HasBonus { get; set; }
        public bool HasLoanFacility { get; set; }
        public bool CoverageIncreasing { get; set; }
        public decimal CoverageIncreaseRate { get; set; }
        public bool IsCoverageUntilAge { get; set; }
        public int? CoverageUntilAge { get; set; }
        public int LoanEligibleAfterYears { get; set; }
        public decimal MaxLoanPercentage { get; set; }
        public decimal LoanInterestRate { get; set; }
        public decimal BonusRate { get; set; }
        public decimal TerminalBonusRate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ReinstatementPenaltyAmount { get; set; }

        [Range(0, 3650)]
        public int ReinstatementDays { get; set; }
    }
}