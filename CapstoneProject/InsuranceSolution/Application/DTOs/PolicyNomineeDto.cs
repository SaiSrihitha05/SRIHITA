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
        public string? NomineeName { get; set; }
        public string? RelationshipToPolicyHolder { get; set; }
        public string? ContactNumber { get; set; }

        [Range(0.01, 100)]
        public decimal SharePercentage { get; set; }
    }
}