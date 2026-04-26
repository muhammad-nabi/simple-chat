using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Application.Presence.Models;

public record PresenceInfo(PresenceStatus Status, string DisplayName);
