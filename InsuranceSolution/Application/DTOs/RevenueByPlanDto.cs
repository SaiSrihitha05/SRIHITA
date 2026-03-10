using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class RevenueByPlanDto
    {
        public string PlanName { get; set; } = string.Empty;
        public string PlanType { get; set; } = string.Empty;
        public int TotalPolicies { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}