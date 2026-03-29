using Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IChatMessageRepository
    {
        Task AddAsync(ChatMessage message);
        Task<List<ChatMessage>> GetBySessionAsync(int? customerId, int? agentId);
        Task<List<ChatMessage>> GetBySessionIdAsync(string sessionId);
        Task<ChatSession> GetOrCreateSessionAsync(string sessionId, int? customerId);
        Task<ChatSession?> GetSessionAsync(string sessionId);
        Task UpdateSessionAsync(ChatSession session);
        Task<List<ChatSession>> GetActiveSessionsForAgentAsync(int agentId);
        Task<List<ChatSession>> GetActiveSessionsForOfficerAsync(int officerId);
        Task LinkSessionToUserAsync(string sessionId, int userId);
        Task<List<ChatSession>> GetByCustomerIdAsync(int customerId);
        Task<List<ChatMessage>> GetRecentBySessionAsync(string sessionId, int limit);
    }
}
