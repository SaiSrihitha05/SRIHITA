using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ReinstateRequestDto
    {
        [Required]
        public string PaymentReference { get; set; } = string.Empty;
    }
}
