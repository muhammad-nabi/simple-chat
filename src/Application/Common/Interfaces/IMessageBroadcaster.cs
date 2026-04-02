using SimpleChat.Application.Messaging.Queries.GetMessageHistory;

namespace SimpleChat.Application.Common.Interfaces;

public interface IMessageBroadcaster
{
    Task BroadcastMessageAsync(MessageDto message, long conversationId, CancellationToken cancellationToken);
}
