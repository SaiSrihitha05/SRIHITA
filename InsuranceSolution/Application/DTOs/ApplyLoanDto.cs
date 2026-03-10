using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ApplyLoanDto
    {
        [Required]
        public int PolicyAssignmentId { get; set; }
    }
}
