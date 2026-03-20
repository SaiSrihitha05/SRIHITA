using System.Collections.Generic;

namespace Application.DTOs
{
    public class EmailRequest
    {
        public string ToEmail { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public List<EmailAttachment>? Attachments { get; set; }
    }

    public class EmailAttachment
    {
        public string Name { get; set; } = string.Empty;
        public byte[] Content { get; set; } = null!;
        public string ContentType { get; set; } = "application/pdf";
    }
}
