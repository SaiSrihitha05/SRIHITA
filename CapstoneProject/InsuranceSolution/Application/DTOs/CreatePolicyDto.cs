using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CreatePolicyDto
    {
        [Required]
        public int PlanId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public PremiumFrequency PremiumFrequency { get; set; }

        [Required]
        [Range(1, 50)]
        public int TermYears { get; set; }

        [Required]
        public string Members { get; set; } = string.Empty;    // JSON string from form

        [Required]
        public string Nominees { get; set; } = string.Empty;   // JSON string from form

        [Required]
        public string Address { get; set; } = string.Empty;

        public IFormFile? IdentityProof { get; set; }
        public IFormFile? IncomeProof { get; set; }
        public List<IFormFile>? MemberDocuments { get; set; }
    }
}