using Domain.Enums;

namespace Domain.Entities
{
    public class InsuranceClaim        // ← renamed from Claim to avoid conflict
    {
        public int Id { get; set; }

        // The policy under which the claim is being filed
        public int PolicyAssignmentId { get; set; }

        // The specific person whose coverage is being claimed
        public int PolicyMemberId { get; set; }

        // The officer assigned to investigate and process this claim
        public int? ClaimsOfficerId { get; set; }

        // Type of claim (Death vs Maturity)
        public ClaimType ClaimType { get; set; }

        // The total sum assured being requested
        public decimal ClaimAmount { get; set; }

        // Name of the person (usually a nominee) requesting the funds
        public string NomineeName { get; set; } = string.Empty;

        // Contact info for the person filing the claim
        public string NomineeContact { get; set; } = string.Empty;

        // Mandatory legal ID for death-related claims
        public string? DeathCertificateNumber { get; set; }

        // When the initial request was submitted
        public DateTime FiledDate { get; set; }

        // Current status (Submitted, UnderReview, Settled, etc.)
        public ClaimStatus Status { get; set; }

        // Reasoning for approval, rejection, or requests for more info
        public string? Remarks { get; set; }

        // The final actual payout amount approved by the officer
        public decimal? SettlementAmount { get; set; }

        // Timestamp for when processing was completed
        public DateTime? ProcessedDate { get; set; }

        // Record creation audit trail
        public DateTime CreatedAt { get; set; }

        // Navigation references
        public PolicyAssignment? PolicyAssignment { get; set; }
        public PolicyMember? PolicyMember { get; set; }
        public User? ClaimsOfficer { get; set; }

        // Supporting evidence (Death Certificates, ID proofs, etc.)
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}