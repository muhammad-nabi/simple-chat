namespace SimpleChat.Application.Messaging.Queries.GetGroupMembers;

public record GroupMemberDto(
    string UserId,
    string DisplayName,
    DateTimeOffset JoinedAt);
