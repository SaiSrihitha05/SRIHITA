using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class PolicyNomineeDto
    {
        [Required]
        public string NomineeName { get; set; } = string.Empty;

        [Required]
        public string RelationshipToPolicyHolder { get; set; } = string.Empty;

        [Required]
        public string ContactNumber { get; set; } = string.Empty;

        [Range(0.01, 100)]
        public decimal SharePercentage { get; set; }
    }
}