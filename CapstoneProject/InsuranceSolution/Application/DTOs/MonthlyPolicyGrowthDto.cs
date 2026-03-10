using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class MonthlyPolicyGrowthDto
    {
        public string MonthName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public int PoliciesCreated { get; set; }
        public int ActivePolicies { get; set; }
    }
}