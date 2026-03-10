using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.DTOs
{
    public class CustomerDashboardDto
    {
        public int TotalPolicies { get; set; }
        public int ActivePolicies { get; set; }
        public int PendingPolicies { get; set; }
        public decimal TotalPremiumPaid { get; set; }
        public DateTime? NextDueDate { get; set; }
        public decimal? NextDueAmount { get; set; }
        public bool IsPaymentDueSoon { get; set; }   // due within 30 days
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public List<PaymentResponseDto> RecentPayments { get; set; } = new();
        public List<PolicyResponseDto> MyPolicies { get; set; } = new();
        public bool HasOverduePayment { get; set; }
        public int DaysOverdue { get; set; } = new();
        public Dictionary<string, int> MyClaimsByStatus { get; set; } = new();
        public List<NotificationResponseDto> RecentNotifications { get; set; } = new();
    }
}