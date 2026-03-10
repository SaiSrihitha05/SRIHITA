using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class AgentPerformanceDto
    {
        public int AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public int PoliciesSold { get; set; }
        public int ActivePolicies { get; set; }
        public decimal TotalPremiumGenerated { get; set; }
        public decimal CommissionEarned { get; set; }
        public int Rank { get; set; }
    }
}