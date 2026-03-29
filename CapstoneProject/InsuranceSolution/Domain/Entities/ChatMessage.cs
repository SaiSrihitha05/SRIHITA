using Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int? CustomerId { get; set; }
        public int? AgentId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        public int? SessionInternalId { get; set; }
        public ChatSession? Session { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public ChatSenderType SenderType { get; set; }
        public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;
        public int? LinkedPlanId { get; set; }
        public string? LinkedPlanUrl { get; set; }
        public string? LinkedPlanName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties (optional)
        public User? Customer { get; set; }
        public User? Agent { get; set; }
    }
}
