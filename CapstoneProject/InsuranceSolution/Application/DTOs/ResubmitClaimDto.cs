using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Application.DTOs
{
    public class ResubmitClaimDto
    {
        public decimal? ClaimAmount { get; set; }
        public string? Remarks { get; set; }
        public List<IFormFile>? Documents { get; set; }
        public List<string>? DocumentCategories { get; set; }
    }
}
