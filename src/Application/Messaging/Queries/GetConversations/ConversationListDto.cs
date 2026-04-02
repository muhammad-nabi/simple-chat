namespace SimpleChat.Application.Messaging.Queries.GetConversations;

public record ConversationListDto(
    long Id,
    string Type,
    string? Name,
    string? LastMessagePreview,
    DateTimeOffset? LastMessageAt,
    List<ParticipantDto> OtherParticipants,
    int UnreadCount);

public record ParticipantDto(string UserId, string DisplayName);
