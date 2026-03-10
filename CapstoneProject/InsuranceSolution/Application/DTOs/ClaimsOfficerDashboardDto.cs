using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class ClaimsOfficerDashboardDto
    {
        public int TotalAssignedClaims { get; set; }
        public int PendingReviewClaims { get; set; }
        public int SettledClaims { get; set; }
        public int RejectedClaims { get; set; }
        public List<ClaimResponseDto> RecentAssignedClaims { get; set; } = new();
        public double AverageProcessingTimeDays { get; set; }  
        public int UrgentClaims { get; set; }                  
        public int ActiveClaimsCount { get; set; }
        public List<MonthlyClaimsProcessedDto> MonthlyProcessed { get; set; } = new();
    }
}