namespace SimpleChat.Application.Messaging.Queries.GetMessageHistory;

public record MessageHistoryResponse(
    List<MessageDto> Messages,
    bool HasMore,
    long? NextCursor);
