namespace Application.DTOs
{
    public class ChatResponseDto
    {
        public string Response { get; set; } = string.Empty;
        public string Intent { get; set; } = "General"; // General, PolicyRelated, ClaimRelated
        public EscalationTargetDto? EscalationTarget { get; set; }
        public ChatActionDto? Action { get; set; } // AI-suggested action

        // NEW — only populated on welcome response or specific guidance
        public List<SuggestedQuestionDto> SuggestedQuestions { get; set; } = new();
    }

    public class ChatActionDto
    {
        public string Type { get; set; } = string.Empty; // e.g., "BuyPlan"
        public int? PlanId { get; set; }
        public string? PlanName { get; set; }
        public string? Url { get; set; }
    }

    public class UpdateClaimStatusDto
    {
        public int ClaimId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class EscalationTargetDto
    {
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? Id { get; set; }
    }
}
