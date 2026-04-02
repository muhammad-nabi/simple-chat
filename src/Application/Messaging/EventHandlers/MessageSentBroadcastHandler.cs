using Microsoft.Extensions.Logging;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.Notifications;
using SimpleChat.Application.Messaging.Queries.GetMessageHistory;

namespace SimpleChat.Application.Messaging.EventHandlers;

public class MessageSentBroadcastHandler(
    IMessageBroadcaster broadcaster,
    IIdentityService identityService,
    ILogger<MessageSentBroadcastHandler> logger) : INotificationHandler<MessageSent>
{
    public async Task Handle(MessageSent notification, CancellationToken cancellationToken)
    {
        try
        {
            Dictionary<string, string> displayNames = await identityService
                .GetDisplayNamesByIdsAsync(new[] { notification.Message.SenderId }, cancellationToken);

            string senderDisplayName = displayNames.GetValueOrDefault(
                notification.Message.SenderId, "Unknown");

            MessageDto messageDto = new(
                notification.Message.Id,
                notification.ConversationId,
                notification.Message.SenderId,
                senderDisplayName,
                notification.Message.Content,
                notification.Message.SentAt,
                notification.Message.MessageType.ToString());

            await broadcaster.BroadcastMessageAsync(messageDto, notification.ConversationId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast message {MessageId} for conversation {ConversationId}",
                notification.Message.Id, notification.ConversationId);
        }
    }
}
