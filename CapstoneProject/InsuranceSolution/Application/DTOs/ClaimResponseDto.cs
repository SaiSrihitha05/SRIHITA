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
        public int PolicyMemberId { get; set; }
        public string PolicyMemberName { get; set; } = string.Empty;
        public int? ClaimsOfficerId { get; set; }
        public string? ClaimsOfficerName { get; set; }
        public string ClaimType { get; set; } = string.Empty;
        public decimal ClaimAmount { get; set; }
        public decimal BaseCoverageAmount { get; set; }
        public decimal AccumulatedBonus { get; set; }
        public decimal TerminalBonus { get; set; }
        public string NomineeName { get; set; } = string.Empty;
        public string NomineeContact { get; set; } = string.Empty;
        public string? DeathCertificateNumber { get; set; }
        public DateTime FiledDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public decimal? SettlementAmount { get; set; }
        public decimal OutstandingLoanAmount { get; set; }
        public decimal NetSettlementAmount { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<DocumentResponseDto> Documents { get; set; } = new();
        public List<ClaimNomineeSettlementDto> SettlementBreakdown { get; set; } = new();
    }
}
