using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class FileClaimDto
    {
        [Required]
        public int PolicyAssignmentId { get; set; }

        [Required]
        public int ClaimForMemberId { get; set; }   // which member passed away

        [Required]
        public ClaimType ClaimType { get; set; }

        // Death claim details
        public string? DeathCertificateNumber { get; set; }
        public DateTime? DateOfDeath { get; set; }
        public string? CauseOfDeath { get; set; }
        public string? PlaceOfDeath { get; set; }

        public string? Remarks { get; set; }

        // Supporting documents (death certificate, medical records)
        public List<IFormFile>? Documents { get; set; }
        public List<string>? DocumentCategories { get; set; }
    }
}