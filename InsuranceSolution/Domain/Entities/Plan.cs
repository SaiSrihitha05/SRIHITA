using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entities
{
    public class Plan
    {
        public int Id { get; set; }

        // The user-friendly name, e.g., "Term Life Protection"
        public string PlanName { get; set; } = string.Empty;

        // Categorization for filtering (e.g., TermLife, Endowment, Savings, WholeLife, Others)
        public PlanCategory PlanType { get; set; }

        // A detailed explanation of what the plan offers
        public string Description { get; set; } = string.Empty;

        // The starting point for premium calculations
        public decimal BaseRate { get; set; }

        // Eligibility constraints for the primary insured
        public int MinAge { get; set; }
        public int MaxAge { get; set; }

        // Financial limits for the total sum assured
        public decimal MinCoverageAmount { get; set; }
        public decimal MaxCoverageAmount { get; set; }

        // Duration limits for how long the policy can run
        public int MinTermYears { get; set; }
        public int MaxTermYears { get; set; }

        // Allowance for late payments before the policy lapses
        public int GracePeriodDays { get; set; }

        // Does the customer get a payout if they survive the term?
        public bool HasMaturityBenefit { get; set; }

        // Special case where premiums are returned at the end
        public bool IsReturnOfPremium { get; set; }

        // Limits for family-based or group policies
        public int MaxPolicyMembersAllowed { get; set; }

        // Nomination constraints to ensure valid legal beneficiary setup
        public int MinNominees { get; set; } = 1;
        public int MaxNominees { get; set; } = 5;

        // Audit trail for product lifecycle
        public DateTime CreatedAt { get; set; }

        // Control if the plan is currently available for purchase
        public bool IsActive { get; set; }

        // The percentage of premium that goes to the sales agent
        public decimal CommissionRate { get; set; }


        public bool HasDeathBenefit { get; set; } = true;
        public bool HasBonus { get; set; } = false;
        public bool HasLoanFacility { get; set; } = false;
        public bool CoverageIncreasing { get; set; } = false;
        public decimal CoverageIncreaseRate { get; set; } = 0;
        public int CoverageUntilAge { get; set; } = 0;
        public int LoanEligibleAfterYears { get; set; } = 0;
        public decimal MaxLoanPercentage { get; set; } = 0;
        public decimal LoanInterestRate { get; set; } = 0;

        public decimal BonusRate { get; set; } = 0;
        public decimal TerminalBonusRate { get; set; } = 0;
    }
}
