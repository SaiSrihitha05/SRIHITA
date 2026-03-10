using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs
{
    public class UpdatePolicyStatusDto
    {
        [Required]
        public PolicyStatus Status { get; set; }

        public string? Remarks { get; set; }
    }
}