using MediatR;
using SimpleChat.Application.Common.Interfaces;

namespace SimpleChat.Application.Identity.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly ISessionService _sessionService;

    public LogoutCommandHandler(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Invalidate only this device's session (single-device logout).
        // If the token doesn't exist in Redis, this is a no-op (idempotent).
        await _sessionService.InvalidateSessionAsync(request.RefreshToken);
    }
}
