using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CreatePaymentDto
    {
        [Required]
        public int PolicyAssignmentId { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;
        // e.g. "CreditCard", "DebitCard", "NetBanking", "UPI"
        [Range(0, 11, ErrorMessage = "Extra installments must be between 0 and 11")]
        public int ExtraInstallments { get; set; } = 0;
    }
}
