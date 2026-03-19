using Domain.Enums;

namespace Application.DTOs
{
    public class PlanFilterDto
    {
        public PlanCategory? PlanType { get; set; }
        public int? Age { get; set; }                        // filters plans where MinAge <= Age <= MaxAge
        public decimal? CoverageAmount { get; set; }         // filters plans where Min <= Amount <= Max
        public int? TermYears { get; set; }                  // filters plans where MinTerm <= Years <= MaxTerm
        public bool? HasMaturityBenefit { get; set; }
        public bool? IsReturnOfPremium { get; set; }
        public bool? HasDeathBenefit { get; set; }
        public bool? HasBonus { get; set; }
        public bool? HasLoanFacility { get; set; }
        public bool? CoverageIncreasing { get; set; }

        public decimal? MaxLoanInterestRate { get; set; }
        public decimal? MinMaxLoanPercentage { get; set; }
        public int? MaxLoanEligibleAfterYears { get; set; }
        public bool? IsCoverageUntilAge { get; set; }
        public decimal? MinCoverageIncreaseRate { get; set; }
    }
}

