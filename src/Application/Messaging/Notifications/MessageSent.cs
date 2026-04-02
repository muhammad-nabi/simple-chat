using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.Messaging.Notifications;

public record MessageSent(Message Message, long ConversationId) : INotification;
