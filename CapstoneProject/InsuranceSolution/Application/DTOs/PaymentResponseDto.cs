using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Application.DTOs
{
    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public int PolicyAssignmentId { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int InstallmentsPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CommissionStatus { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public DateTime NextDueDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}