using MediatR;
using Microsoft.Extensions.Logging;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Identity.Constants;

namespace SimpleChat.Application.Identity.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        ISessionService sessionService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var oldRefreshToken = request.RefreshToken;

        // Look up session by refresh token
        var userId = await _sessionService.GetSessionUserIdAsync(oldRefreshToken);

        if (userId == null)
        {
            // Token not found in active sessions — check if it was already rotated out (reuse detection)
            var reusedUserId = await _sessionService.GetUsedTokenUserIdAsync(oldRefreshToken);
            if (reusedUserId != null)
            {
                // TOKEN REUSE DETECTED — emergency lockout: invalidate ALL sessions for this user
                _logger.LogWarning("Refresh token reuse detected for user {UserId}. Invalidating all sessions (potential token theft).", reusedUserId);
                await _sessionService.InvalidateAllSessionsAsync(reusedUserId);
            }

            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        // Load user and verify still active
        var userInfo = await _identityService.FindUserByIdAsync(userId);
        if (userInfo == null || !userInfo.Value.IsActive)
        {
            await _sessionService.InvalidateSessionAsync(oldRefreshToken);
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var (_, displayName, email, role, _) = userInfo.Value;

        // Generate new tokens
        var (accessToken, newRefreshToken) = _jwtTokenService.GenerateTokens(
            userId, email, displayName, role);

        // Mark old token as used (for reuse detection) before invalidating
        var expiry = TimeSpan.FromDays(TokenConstants.RefreshTokenExpiryDays);

        // Mark old token as used (for reuse detection) before invalidating
        await _sessionService.MarkTokenAsUsedAsync(oldRefreshToken, userId, expiry);

        // Invalidate old session
        await _sessionService.InvalidateSessionAsync(oldRefreshToken);

        // Store new session
        await _sessionService.StoreSessionAsync(userId, newRefreshToken, expiry);

        return new RefreshTokenResponse(
            accessToken, newRefreshToken, userId, displayName, email, role.ToString());
    }
}
