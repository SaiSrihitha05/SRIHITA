using System;

namespace Application.DTOs
{
    public class LoanRepaymentDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public decimal PrincipalPaid { get; set; }
        public decimal InterestPaid { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime RepaymentDate { get; set; }
    }
}
