using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MonthlyClaimsProcessedDto
{
    public string MonthName { get; set; } = string.Empty;
    public int ClaimsProcessed { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
}