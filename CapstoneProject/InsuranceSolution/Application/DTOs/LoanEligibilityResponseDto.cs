namespace Application.DTOs
{
    public class LoanEligibilityResponseDto
    {
        public bool Eligible { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal EstimatedAmount { get; set; }
        public decimal InterestRate { get; set; }
    }
}
