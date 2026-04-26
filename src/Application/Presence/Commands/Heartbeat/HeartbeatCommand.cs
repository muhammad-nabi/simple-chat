using SimpleChat.Application.Common.Security;
using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Application.Presence.Commands.Heartbeat;

[Authorize]
public record HeartbeatCommand(PresenceStatus Status) : IRequest<Unit>;
