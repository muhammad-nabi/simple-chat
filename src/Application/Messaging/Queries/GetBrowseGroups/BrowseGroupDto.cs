namespace SimpleChat.Application.Messaging.Queries.GetBrowseGroups;

public record BrowseGroupDto(
    long Id,
    string Name,
    int ParticipantCount,
    string? LastMessagePreview,
    DateTimeOffset? LastMessageAt);
