using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class MonthlyCommissionDto
    {
        public string MonthName { get; set; } = string.Empty;
        public int PoliciesSold { get; set; }
        public decimal CommissionEarned { get; set; }
        public bool BonusApplied { get; set; }
    }
}