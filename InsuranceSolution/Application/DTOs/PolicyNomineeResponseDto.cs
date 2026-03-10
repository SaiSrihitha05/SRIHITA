using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class PolicyNomineeResponseDto
    {
        public int Id { get; set; }
        public string NomineeName { get; set; } = string.Empty;
        public string RelationshipToPolicyHolder { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public decimal SharePercentage { get; set; }
    }
}