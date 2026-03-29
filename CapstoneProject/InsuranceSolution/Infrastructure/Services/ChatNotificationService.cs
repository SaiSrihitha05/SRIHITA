using Application.Interfaces.Services;
using Domain.Enums;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ChatNotificationService : IChatNotificationService
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatNotificationService(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyMessageAsync(string sessionId, string message, ChatSenderType senderType, string? intent = null)
        {
            await _hubContext.Clients.Group(sessionId).SendAsync("ReceiveMessage", new
            {
                Message = message,
                SenderType = senderType.ToString(),
                Intent = intent,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task NotifyChatClosedAsync(string sessionId, bool isFullClose = true)
        {
            await _hubContext.Clients.Group(sessionId).SendAsync("ChatClosed", new
            {
                sessionId = sessionId,
                isFullClose = isFullClose,
                timestamp = DateTime.UtcNow
            });
        }

        public async Task NotifyPlanLinkAsync(string sessionId, object planLinkData)
        {
            await _hubContext.Clients.Group(sessionId).SendAsync("ReceivePlanLink", planLinkData);
        }
    }
}
