using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.DTOs
{
    public class PolicyMemberResponseDto
    {
        public int Id { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string RelationshipToCustomer { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public decimal CoverageAmount { get; set; }
        public bool IsSmoker { get; set; }
        public bool HasPreExistingDiseases { get; set; }
        public string? DiseaseDescription { get; set; }
        public string Occupation { get; set; } = string.Empty;
        public bool IsPrimaryInsured { get; set; }
        public decimal CurrentCoverageAmount { get; set; }
        public int YearsActive { get; set; }
    }
}