using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class ChatSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty; // GUID tracking

        public int? CustomerId { get; set; } // Nullable for guests
        
        public int? AgentId { get; set; } // Assigned agent
        public int? ClaimsOfficerId { get; set; } // Assigned claims officer
        public bool IsAgentAssigned { get; set; } = false;
        public bool IsClaimsOfficerAssigned { get; set; } = false;
        public int? RelatedClaimId { get; set; } // Linked claim for context
        public int? RelatedPolicyId { get; set; } // Linked policy for agent context
        public int? LinkedPolicyId { get; set; } // Explicitly extracted policy ID

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ClosedAt { get; set; }
        public bool IsChatClosed { get; set; } = false;

        // Navigation
        public InsuranceClaim? RelatedClaim { get; set; }
        public PolicyAssignment? RelatedPolicy { get; set; }
    }
}
