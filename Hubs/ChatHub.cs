using Microsoft.AspNetCore.SignalR;

namespace olx_be_api.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinRoom(string chatRoomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomId);
        }

        public async Task SendMessageToRoom(string chatRoomId, string message)
        {
            await Clients.Group(chatRoomId).SendAsync("ReceiveMessage", message);
        }
    }
}
