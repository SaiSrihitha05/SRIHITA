using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PolicyMember
    {
        public int Id { get; set; }

        // Link back to the main policy contract
        public int PolicyAssignmentId { get; set; }

        // Full legal name of the member
        public string MemberName { get; set; } = string.Empty;

        // How this person is related to the customer (e.g., Self, Spouse, Child)
        public string RelationshipToCustomer { get; set; } = string.Empty;

        // Birth date, used for age-based premium calculations
        public DateTime DateOfBirth { get; set; }

        // Gender of the member, another factor in premium rates
        public string Gender { get; set; } = string.Empty;

        // The specific coverage amount allocated to this member
        public decimal CoverageAmount { get; set; }

        // Risk factor: Is the member a smoker?
        public bool IsSmoker { get; set; }

        // Health declaration for underwriting
        public bool HasPreExistingDiseases { get; set; }

        // Details about any declared illnesses
        public string? DiseaseDescription { get; set; }

        // Professional background for risk assessment
        public string Occupation { get; set; } = string.Empty;

        // Flag to identify the main person responsible for the policy
        public bool IsPrimaryInsured { get; set; }

        // Tracks if the member has died and their coverage consumed
        public Domain.Enums.MemberStatus Status { get; set; } = Domain.Enums.MemberStatus.Active;

        // Audit timestamp
        public DateTime CreatedAt { get; set; }

        // Navigation back to the parent policy
        public PolicyAssignment? PolicyAssignment { get; set; }
    }
}
