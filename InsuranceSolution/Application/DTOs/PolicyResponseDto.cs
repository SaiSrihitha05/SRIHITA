using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class PolicyResponseDto
    {
        public int Id { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? AgentId { get; set; }
        public string? AgentName { get; set; }
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TermYears { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CommissionStatus { get; set; } = string.Empty;
        public decimal TotalPremiumAmount { get; set; }
        public string PremiumFrequency { get; set; } = string.Empty;
        public DateTime NextDueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PolicyMemberResponseDto> Members { get; set; } = new();
        public List<PolicyNomineeResponseDto> Nominees { get; set; } = new();
        public List<DocumentResponseDto> Documents { get; set; } = new();
    }
}