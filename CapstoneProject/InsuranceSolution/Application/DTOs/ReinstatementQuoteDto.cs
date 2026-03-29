using System;

namespace Application.DTOs
{
    public class ReinstatementQuoteDto
    {
        public int PolicyId { get; set; }
        public string PolicyNumber { get; set; } = string.Empty;
        public int MissedMonths { get; set; }
        public decimal MonthlyPremium { get; set; }
        public decimal MissedPremiumTotal { get; set; }
        public decimal PenaltyAmount { get; set; }
        public decimal TotalAmountDue { get; set; }
        public DateTime QuoteValidUntil { get; set; }
        public int DaysRemainingToReinstate { get; set; }
    }
}
