using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        // The user who will receive this notification
        public int UserId { get; set; }

        // A brief summary line for the alert
        public string Title { get; set; } = string.Empty;

        // The detailed body text of the alert
        public string Message { get; set; } = string.Empty;

        // Categorization to help the UI display the right icon/color
        public NotificationType Type { get; set; }

        // Track whether the user has seen this alert yet
        public bool IsRead { get; set; }

        // Audit timestamp
        public DateTime CreatedAt { get; set; }

        // Optional links to related entities for deep-linking in the UI
        public int? PolicyAssignmentId { get; set; }
        public int? ClaimId { get; set; }
        public int? PaymentId { get; set; }

        // Navigation back to the recipient
        public User? User { get; set; }
    }
}
