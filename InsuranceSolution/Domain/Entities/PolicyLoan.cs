using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PolicyLoan
    {
        public int Id { get; set; }
        public int PolicyAssignmentId { get; set; }
        public int CustomerId { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal InterestRate { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal TotalInterestPaid { get; set; }
        public LoanStatus Status { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public PolicyAssignment PolicyAssignment { get; set; } = null!;
        public User Customer { get; set; } = null!;
        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();
    }
}
