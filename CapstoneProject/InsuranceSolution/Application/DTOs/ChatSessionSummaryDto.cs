namespace Application.DTOs
{
    public class ChatSessionSummaryDto
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public int? ClaimsOfficerId { get; set; }
        public int? RelatedClaimId { get; set; }
        public int? AgentId { get; set; }
        public int? RelatedPolicyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
