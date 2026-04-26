using Microsoft.Extensions.Logging;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Presence.Models;

namespace SimpleChat.Application.Presence.Commands.Heartbeat;

public class HeartbeatCommandHandler : IRequestHandler<HeartbeatCommand, Unit>
{
    private readonly IUser _currentUser;
    private readonly IIdentityService _identityService;
    private readonly ICacheService _cache;
    private readonly ILogger<HeartbeatCommandHandler> _logger;

    private const string PresenceKeyPrefix = "presence:";
    private const string OnlineUsersKey = "online_users";
    private static readonly TimeSpan HeartbeatTtl = TimeSpan.FromSeconds(90);

    public HeartbeatCommandHandler(
        IUser currentUser,
        IIdentityService identityService,
        ICacheService cache,
        ILogger<HeartbeatCommandHandler> logger)
    {
        _currentUser = currentUser;
        _identityService = identityService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Unit> Handle(HeartbeatCommand request, CancellationToken cancellationToken)
    {
        string userId = Guard.Against.NullOrWhiteSpace(_currentUser.Id);

        Dictionary<string, string> displayNames = await _identityService
            .GetDisplayNamesByIdsAsync(new[] { userId }, cancellationToken);
        string displayName = displayNames.TryGetValue(userId, out string? resolved)
            && !string.IsNullOrWhiteSpace(resolved)
                ? resolved
                : "Unknown";

        PresenceInfo presenceInfo = new(request.Status, displayName);

        await _cache.SetAsync(
            PresenceKeyPrefix + userId,
            presenceInfo,
            HeartbeatTtl,
            cancellationToken);

        await _cache.SetAddAsync(OnlineUsersKey, userId, cancellationToken);

        _logger.LogDebug("Heartbeat received from user {UserId} with status {Status}", userId, request.Status);

        return Unit.Value;
    }
}
