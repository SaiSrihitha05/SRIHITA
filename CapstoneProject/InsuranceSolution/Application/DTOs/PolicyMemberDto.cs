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
        public string? MemberName { get; set; }
        public string? RelationshipToCustomer { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal CoverageAmount { get; set; }

        public bool IsSmoker { get; set; }
        public bool HasPreExistingDiseases { get; set; }
        public string? DiseaseDescription { get; set; }

        public string? Occupation { get; set; }

        public bool IsPrimaryInsured { get; set; }
        public List<IFormFile>? MemberDocuments { get; set; }
    }
}