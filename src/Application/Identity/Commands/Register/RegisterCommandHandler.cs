using MediatR;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Domain.Common.Enums;
using ValidationException = SimpleChat.Application.Common.Exceptions.ValidationException;
using FluentValidation.Results;

namespace SimpleChat.Application.Identity.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService _sessionService;

    public RegisterCommandHandler(
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        ISessionService sessionService)
    {
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check for duplicate email
        if (await _identityService.EmailExistsAsync(request.Email))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure("Email", "An account with this email already exists.")
            });
        }

        // First user gets Admin role, subsequent users get Member
        var role = await _identityService.AnyUsersExistAsync()
            ? UserRole.Member
            : UserRole.Admin;

        // Create user (bcrypt password hasher kicks in via DI)
        var (result, userId) = await _identityService.CreateUserAsync(
            request.Email, request.DisplayName, request.Password, role);

        if (!result.Succeeded)
        {
            throw new ValidationException(
                result.Errors.Select(e => new ValidationFailure(string.Empty, e)));
        }

        // Generate JWT tokens
        var (accessToken, refreshToken) = _jwtTokenService.GenerateTokens(
            userId, request.Email, request.DisplayName, role);

        // Store refresh token session in Redis
        await _sessionService.StoreSessionAsync(userId, refreshToken, TimeSpan.FromDays(7));

        return new RegisterResponse(
            accessToken, refreshToken, userId, request.DisplayName, request.Email, role.ToString());
    }
}
