using MediatR;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Identity.Constants;

namespace SimpleChat.Application.Identity.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService _sessionService;

    public LoginCommandHandler(
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        ISessionService sessionService)
    {
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find user by email — generic error if not found
        var userInfo = await _identityService.FindUserByEmailAsync(request.Email);
        if (userInfo == null)
        {
            // Perform dummy password check to equalize timing with the "user exists" path,
            // preventing user enumeration via response time analysis.
            await _identityService.VerifyDummyPasswordAsync(request.Password);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var (userId, displayName, email, role, isActive) = userInfo.Value;

        // Verify password — generic error if wrong
        var passwordValid = await _identityService.CheckPasswordAsync(userId, request.Password);
        if (!passwordValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Reject deactivated users — same generic error (NFR13)
        if (!isActive)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Generate JWT tokens
        var (accessToken, refreshToken) = _jwtTokenService.GenerateTokens(
            userId, email, displayName, role);

        // Store refresh token session in Redis
        await _sessionService.StoreSessionAsync(userId, refreshToken, TimeSpan.FromDays(TokenConstants.RefreshTokenExpiryDays));

        return new LoginResponse(
            accessToken, refreshToken, userId, displayName, email, role.ToString());
    }
}
