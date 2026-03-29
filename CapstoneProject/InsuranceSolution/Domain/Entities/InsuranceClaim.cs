using Domain.Enums;

namespace Domain.Entities
{
    public class InsuranceClaim        
    {
        public int Id { get; set; }

        public int PolicyAssignmentId { get; set; }

        public int ClaimForMemberId { get; set; }

        public string IssuedAuthority { get; set; } = string.Empty;
        public bool VerifiedByOfficer { get; set; } = false;
        public DateTime? VerificationDate { get; set; }

        public int? ClaimsOfficerId { get; set; }

        public ClaimType ClaimType { get; set; }

        public decimal ClaimAmount { get; set; }

        public string? DeathCertificateNumber { get; set; }

        public DateTime? DateOfDeath { get; set; }
        public string? CauseOfDeath { get; set; }
        public string? PlaceOfDeath { get; set; }

        public DateTime FiledDate { get; set; }

        public ClaimStatus Status { get; set; }

        public string? Remarks { get; set; }
        public string? OfficerRemarks { get; set; }
        public string? RejectionReason { get; set; }
        public int ResubmissionCount { get; set; } = 0;
        public DateTime? ResubmissionDeadline { get; set; }

        public decimal? SettlementAmount { get; set; }

        public DateTime? ProcessedDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public PolicyAssignment? PolicyAssignment { get; set; }
        public PolicyMember? ClaimMember { get; set; }
        public User? ClaimsOfficer { get; set; }

        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}