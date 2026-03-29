using System.Collections.Generic;

namespace Application.DTOs
{
    public class PlanComparisonResponseDto
    {
        public string Summary { get; set; } = string.Empty;
        public IEnumerable<PlanResponseDto> Plans { get; set; } = new List<PlanResponseDto>();
    }
}
