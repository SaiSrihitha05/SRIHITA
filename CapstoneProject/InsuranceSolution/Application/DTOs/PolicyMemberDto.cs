using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class PolicyMemberDto
    {
        [Required]
        public string MemberName { get; set; } = string.Empty;

        [Required]
        public string RelationshipToCustomer { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal CoverageAmount { get; set; }

        public bool IsSmoker { get; set; }
        public bool HasPreExistingDiseases { get; set; }
        public string? DiseaseDescription { get; set; }

        [Required]
        public string Occupation { get; set; } = string.Empty;

        public bool IsPrimaryInsured { get; set; }
        public List<IFormFile>? MemberDocuments { get; set; }
    }
}