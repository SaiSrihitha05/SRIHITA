using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Infrastructure.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinSession(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }

        public async Task LeaveSession(string sessionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        }
    }
}
