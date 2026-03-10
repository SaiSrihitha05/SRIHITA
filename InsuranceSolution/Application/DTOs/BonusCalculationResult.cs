using System;

namespace Application.DTOs
{
    public class BonusCalculationResult
    {
        public decimal SumAssured { get; set; } = 0;
        public decimal BonusPerYear { get; set; } = 0;
        public int YearsActive { get; set; } = 0;
        public decimal TotalBonus { get; set; } = 0;
        public decimal TerminalBonus { get; set; } = 0;
        public decimal TotalMaturityPayout { get; set; } = 0;
        public decimal BonusRate { get; set; } = 0;
        public decimal TerminalBonusRate { get; set; } = 0;
    }
}
