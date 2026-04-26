using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Application.Presence.Commands.Heartbeat;

public class HeartbeatCommandValidator : AbstractValidator<HeartbeatCommand>
{
    public HeartbeatCommandValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status must be a valid PresenceStatus value.")
            .Must(s => s != PresenceStatus.Offline)
            .WithMessage("Heartbeat status cannot be Offline. Only Online or Away are valid.");
    }
}
