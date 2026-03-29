using System.Collections.Generic;

namespace Application.DTOs
{
    public class ChatMessageDto
    {
        public string Message { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public List<string>? Context { get; set; }
    }
}
