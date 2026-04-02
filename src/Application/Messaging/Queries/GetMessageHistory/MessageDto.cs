namespace SimpleChat.Application.Messaging.Queries.GetMessageHistory;

public record MessageDto(
    long Id,
    long ConversationId,
    string SenderId,
    string SenderDisplayName,
    string Content,
    DateTimeOffset SentAt,
    string MessageType);
