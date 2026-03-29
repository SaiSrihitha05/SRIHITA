using System;
using System.Threading.Tasks;
using Domain.Enums;

namespace Application.Interfaces.Services
{
    public interface IChatNotificationService
    {
        Task NotifyMessageAsync(string sessionId, string message, ChatSenderType senderType, string? intent = null);
        Task NotifyPlanLinkAsync(string sessionId, object planLinkData);
        Task NotifyChatClosedAsync(string sessionId, bool isFullClose = true);
    }
}
