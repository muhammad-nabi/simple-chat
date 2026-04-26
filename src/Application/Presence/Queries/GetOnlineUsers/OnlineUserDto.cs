using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Application.Presence.Queries.GetOnlineUsers;

public record OnlineUserDto(string UserId, string DisplayName, PresenceStatus Status);
