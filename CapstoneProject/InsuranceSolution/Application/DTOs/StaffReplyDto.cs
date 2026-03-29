namespace Application.DTOs
{
    public class StaffReplyDto
    {
        public int? CustomerId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
