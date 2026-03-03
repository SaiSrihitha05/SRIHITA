using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Payment
    {
        public int Id { get; set; }

        // The policy this payment is being applied to
        public int PolicyAssignmentId { get; set; }

        // The exact dollar amount paid
        public decimal Amount { get; set; }

        // When the transaction was successfully processed
        public DateTime PaymentDate { get; set; }

        // How the money was sent (e.g., Credit Card, UPI, NetBanking)
        public string PaymentMethod { get; set; } = string.Empty;

        // Unique ID from the payment gateway to track the transaction externally
        public string TransactionReference { get; set; } = string.Empty;

        // How many billing cycles this payment covers (usually 1, but can be pre-paid)
        public int InstallmentsPaid { get; set; } = 1;

        // Final outcome (Completed, Failed, Pending)
        public PaymentStatus Status { get; set; }

        // Unique business invoice identifier for tax and receipt purposes
        public string InvoiceNumber { get; set; } = string.Empty;

        // Record creation audit trail
        public DateTime CreatedAt { get; set; }

        // Link back to the parent policy contract
        public PolicyAssignment? PolicyAssignment { get; set; }
    }
}
