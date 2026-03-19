using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class RepayLoanDto
    {
        [Required]
        public int PolicyLoanId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
