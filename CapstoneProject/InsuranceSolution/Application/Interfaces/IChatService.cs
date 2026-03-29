using Application.DTOs;
using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponseDto> ProcessMessageAsync(int? userId, ChatMessageDto chatMessage);
        Task<List<ChatSessionSummaryDto>> GetAgentSessionsAsync(int agentId);
        Task<List<ChatSessionSummaryDto>> GetOfficerSessionsAsync(int officerId);
        Task<List<ChatMessage>> GetChatHistoryAsync(int agentId, int customerId);
        Task<List<ChatMessage>> GetSessionHistoryAsync(string sessionId);
        Task AgentReplyAsync(int agentId, int? customerId, string sessionId, string message);
        Task OfficerReplyAsync(int officerId, int? customerId, string sessionId, string message);
        Task SendPlanLinkAsync(int agentId, string sessionId, int planId);
        Task CloseSessionAsync(string sessionId);
        Task<List<ChatMessage>> GetCustomerHistoryAsync(int customerId);
        Task<int?> GetLastAgentIdAsync(int customerId);
        Task LinkSessionToUserAsync(string sessionId, int userId);
        Task<object?> GetClaimContextAsync(int claimId);
        Task UpdateClaimStatusAsync(int officerId, int claimId, string sessionId, string status);
        Task<PolicyContextDto?> GetPolicyContextAsync(string policyNumber);
        Task<PolicyContextDto?> GetPolicyContextByIdAsync(int policyId);
        Task<IEnumerable<object>> GetCustomerPoliciesAsync(int customerId);

        // NEW
        Task<string> GetUserNameAsync(int userId);
        Task<ChatSession?> GetChatSessionAsync(string sessionId);
        Task<ChatResponseDto> GetWelcomeAsync(int? userId, string sessionId);
    }
}
