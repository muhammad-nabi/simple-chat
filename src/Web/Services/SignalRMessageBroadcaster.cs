using Microsoft.AspNetCore.SignalR;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.Queries.GetMessageHistory;
using SimpleChat.Web.Hubs;

namespace SimpleChat.Web.Services;

public class SignalRMessageBroadcaster(IHubContext<ChatHub> hubContext) : IMessageBroadcaster
{
    public async Task BroadcastMessageAsync(MessageDto message, long conversationId, CancellationToken cancellationToken)
    {
        await hubContext.Clients
            .Group(conversationId.ToString())
            .SendAsync("ReceiveMessage", message, cancellationToken);
    }
}
