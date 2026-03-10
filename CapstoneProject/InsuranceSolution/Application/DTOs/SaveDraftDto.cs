using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Application/DTOs/SaveDraftDto.cs
namespace Application.DTOs
{
    public class SaveDraftDto
    {
        public int? PlanId { get; set; }
        public DateTime? StartDate { get; set; }
        public int? TermYears { get; set; }
        public PremiumFrequency? PremiumFrequency { get; set; }

        // Members are optional for draft
        public List<PolicyMemberDto> Members { get; set; }

        // Nominees are optional for draft
        public List<PolicyNomineeDto> Nominees { get; set; }
    }
}