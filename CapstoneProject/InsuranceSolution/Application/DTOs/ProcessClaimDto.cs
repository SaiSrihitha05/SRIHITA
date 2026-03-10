using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs
{
    public class ProcessClaimDto
    {
        [Required]
        public ClaimStatus Status { get; set; }  // Approved or Rejected

        public string? Remarks { get; set; }

        // Only required if Approved
        public decimal? SettlementAmount { get; set; }
    }
}