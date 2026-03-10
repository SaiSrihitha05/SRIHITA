using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PolicyAssignment
    {
        public int Id { get; set; }

        // Unique policy identifier generated upon creation
        public string PolicyNumber { get; set; } = string.Empty;

        // The customer who owns this policy
        public int CustomerId { get; set; }

        // The agent assigned to assist the customer (if any)
        public int? AgentId { get; set; }

        // The underlying plan product being purchased
        public int PlanId { get; set; }

        // When the coverage officially begins
        public DateTime StartDate { get; set; }

        // When the coverage officially ends
        public DateTime EndDate { get; set; }

        // Current lifecycle state (Active, Lapsed, Matured, etc.)
        public PolicyStatus Status { get; set; }

        public CommissionStatus CommissionStatus { get; set; } = CommissionStatus.Pending;

        // The total calculated premium across all covered members
        public decimal TotalPremiumAmount { get; set; }

        // How often the customer pays (Monthly, Quarterly, Yearly)
        public PremiumFrequency PremiumFrequency { get; set; }

        // The date the next payment installment is expected
        public DateTime NextDueDate { get; set; }

        // Record creation timestamp
        public DateTime CreatedAt { get; set; }

        // Timestamp for when an agent was assigned to this policy
        public DateTime? AssignedDate { get; set; }

        // Navigation properties for easy data access
        public User? Customer { get; set; }
        public User? Agent { get; set; }
        public Plan? Plan { get; set; }

        // The duration selected by the customer within the plan's limits
        public int TermYears { get; set; }

        // The group of people (family, etc.) covered under this single policy
        public ICollection<PolicyMember> PolicyMembers { get; set; } = new List<PolicyMember>();

        // The legal beneficiaries who will receive payouts
        public ICollection<PolicyNominee> PolicyNominees { get; set; } = new List<PolicyNominee>();

        // Physical or digital proofs uploaded during the application
        public ICollection<Document> Documents { get; set; } = new List<Document>();

        // ✅ NEW NAVIGATION
        public ICollection<PolicyLoan> Loans { get; set; } = new List<PolicyLoan>();

        // Ensure Payments navigation exists for surrender value calculation
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
