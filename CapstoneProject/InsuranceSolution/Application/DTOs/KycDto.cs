using Microsoft.AspNetCore.Http;
using System;

namespace Application.DTOs
{
    public class ProcessKycDto
    {
        public int? TargetId { get; set; } // Optional: UserId or PolicyMemberId
        public string IdProofType { get; set; } = string.Empty; // Aadhaar, PAN, Passport
        public string IdProofNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty; // Provided name to match against
        public string Gender { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public DateTime? ExpectedDateOfDeath { get; set; } // For Death Cert
        public IFormFile File { get; set; } = null!;
    }

    public class KycResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string KycStatus { get; set; } = string.Empty;
        public string ExtractedName { get; set; } = string.Empty;
        public string ExtractedIdNumber { get; set; } = string.Empty; // Aadhaar_no or Pan_no
        public string ExtractedDate { get; set; } = string.Empty; // dob or date_of_death
        public string ExtractedGender { get; set; } = string.Empty;
        public string ExtractedAuthority { get; set; } = string.Empty;
        public string ExtractedPlace { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
    }
    public class DeathCertificateKycDto
    {
        public IFormFile File { get; set; } = null!;
        public string CertificateNumber { get; set; } = string.Empty;
        public string? DateOfDeath { get; set; }
        public string? DeceasedName { get; set; }
        public string? PlaceOfDeath { get; set; }
    }

    public class NomineeVerificationDto
    {
        public IFormFile File { get; set; } = null!;
        public string ExpectedName { get; set; } = string.Empty;
    }
}
