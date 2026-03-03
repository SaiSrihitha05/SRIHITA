using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PolicyNominee
    {
        public int Id { get; set; }

        // Link back to the policy where they are named as a beneficiary
        public int PolicyAssignmentId { get; set; }

        // Full legal name of the nominee
        public string NomineeName { get; set; } = string.Empty;

        // Family or legal relationship to the policy holder
        public string RelationshipToPolicyHolder { get; set; } = string.Empty;

        // Active contact detail for notification during claim processing
        public string ContactNumber { get; set; } = string.Empty;

        // The portion of the benefits (out of 100%) this nominee is entitled to
        public decimal SharePercentage { get; set; }

        // Timestamp for when the nominee was added
        public DateTime CreatedAt { get; set; }

        // Navigation back to the parent policy
        public PolicyAssignment? PolicyAssignment { get; set; }
    }
}
