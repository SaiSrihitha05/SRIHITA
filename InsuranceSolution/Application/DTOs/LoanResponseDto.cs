using System;
using System.Collections.Generic;

namespace Application.DTOs
{
    public class LoanResponseDto
    {
        public int Id { get; set; }
        public int PolicyAssignmentId { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal LoanAmount { get; set; }
        public decimal InterestRate { get; set; }
        public decimal OutstandingBalance { get; set; }
        public decimal TotalInterestPaid { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LoanDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public List<LoanRepaymentDto> Repayments { get; set; } = new();
    }
}
