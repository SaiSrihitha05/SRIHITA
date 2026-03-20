using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    public class ClaimNomineeSettlementDto
    {
        public string NomineeName { get; set; } = string.Empty;
        public decimal SharePercentage { get; set; }
        public decimal SettlementAmount { get; set; }
    }

    public class ClaimResponseDto
    {
        public int Id { get; set; }
        public int PolicyAssignmentId { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        
        // Customer Info
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;

        // Policy Info Summary
        public string PlanName { get; set; } = string.Empty;
        public string PolicyStatus { get; set; } = string.Empty;
        public DateTime PolicyStartDate { get; set; }
        public int PolicyTerm { get; set; }
        public decimal TotalPolicyCoverage { get; set; }
        public decimal RemainingCoverage { get; set; }

        // Deceased Member Details
        public int ClaimForMemberId { get; set; }
        public string ClaimForMemberName { get; set; } = string.Empty;
        public string MemberRelationship { get; set; } = string.Empty;
        public DateTime? MemberDob { get; set; }
        public int MemberAge { get; set; }
        public string MemberStatus { get; set; } = string.Empty;

        // Premium Payment History
        public DateTime? LastPaymentDate { get; set; }
        public int TotalPaymentsCount { get; set; }
        public decimal PremiumAmount { get; set; }
        public DateTime NextDueDate { get; set; }

        // Officer / Status Info
        public int? ClaimsOfficerId { get; set; }
        public string? ClaimsOfficerName { get; set; }
        public string ClaimType { get; set; } = string.Empty;
        public decimal ClaimAmount { get; set; }
        public decimal BaseCoverageAmount { get; set; }
        public decimal AccumulatedBonus { get; set; }
        public decimal TerminalBonus { get; set; }

        public string? DeathCertificateNumber { get; set; }
        public DateTime? DateOfDeath { get; set; }
        public string? CauseOfDeath { get; set; }
        public string? PlaceOfDeath { get; set; }

        public string IssuedAuthority { get; set; } = string.Empty;
        public bool VerifiedByOfficer { get; set; } = false;
        public DateTime? VerificationDate { get; set; }

        public DateTime FiledDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string? OfficerRemarks { get; set; }
        public decimal? SettlementAmount { get; set; }
        public decimal OutstandingLoanAmount { get; set; }
        public decimal NetSettlementAmount { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public DateTime CreatedAt { get; set; }

        // Detailed Lists
        public List<DocumentResponseDto> Documents { get; set; } = new();
        public List<ClaimNomineeSettlementDto> SettlementBreakdown { get; set; } = new();
        public List<PolicyMemberResponseDto> AllMembers { get; set; } = new();
        public List<PolicyNomineeResponseDto> AllNominees { get; set; } = new();
    }
}
