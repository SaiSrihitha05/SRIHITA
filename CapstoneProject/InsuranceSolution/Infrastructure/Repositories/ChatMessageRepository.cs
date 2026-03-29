using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly InsuranceDbContext _context;

        public ChatMessageRepository(InsuranceDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ChatMessage message)
        {
            await _context.ChatMessages.AddAsync(message);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ChatMessage>> GetBySessionAsync(int? customerId, int? agentId)
        {
            if (customerId != null && agentId == null)
            {
                return await _context.ChatMessages
                    .Where(m => m.CustomerId == customerId)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();
            }

            return await _context.ChatMessages
                .Where(m => (m.CustomerId == customerId && m.AgentId == agentId) || 
                            (m.CustomerId == customerId && m.AgentId == null))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ChatMessage>> GetBySessionIdAsync(string sessionId)
        {
            return await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<ChatSession> GetOrCreateSessionAsync(string sessionId, int? customerId)
        {
            var session = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
            {
                session = new ChatSession
                {
                    SessionId = sessionId,
                    CustomerId = customerId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.ChatSessions.AddAsync(session);
                await _context.SaveChangesAsync();
            }
            else if (customerId != null && session.CustomerId == null)
            {
                // Link guest session to user
                session.CustomerId = customerId;
                await _context.SaveChangesAsync();
            }

            return session;
        }

        public async Task<ChatSession?> GetSessionAsync(string sessionId)
        {
            return await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);
        }

        public async Task UpdateSessionAsync(ChatSession session)
        {
            _context.ChatSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ChatSession>> GetActiveSessionsForAgentAsync(int agentId)
        {
            return await _context.ChatSessions
                .Where(s => s.AgentId == agentId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ChatSession>> GetActiveSessionsForOfficerAsync(int officerId)
        {
            return await _context.ChatSessions
                .Where(s => s.ClaimsOfficerId == officerId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task LinkSessionToUserAsync(string sessionId, int userId)
        {
            var session = await _context.ChatSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session != null && session.CustomerId == null)
            {
                session.CustomerId = userId;
                
                var messages = await _context.ChatMessages
                    .Where(m => m.SessionId == sessionId && m.CustomerId == null)
                    .ToListAsync();
                
                foreach (var msg in messages)
                {
                    msg.CustomerId = userId;
                }
                
                await _context.SaveChangesAsync();
            }
        }
        public async Task<List<ChatSession>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.ChatSessions
                .Where(s => s.CustomerId == customerId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ChatMessage>> GetRecentBySessionAsync(string sessionId, int limit)
        {
            return await _context.ChatMessages
                .Where(m => m.SessionId == sessionId && m.SenderType != ChatSenderType.System)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}
