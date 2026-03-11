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
        public int PolicyMemberId { get; set; }   // which member passed away

        [Required]
        public ClaimType ClaimType { get; set; }

        // For death claims
        public string? DeathCertificateNumber { get; set; }
        public string? NomineeName { get; set; }
        public string? NomineeContact { get; set; }

        public string? Remarks { get; set; }

        // Supporting documents (death certificate, medical records)
        public List<IFormFile>? Documents { get; set; }
    }
}