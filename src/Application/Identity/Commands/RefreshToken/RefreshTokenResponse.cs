namespace SimpleChat.Application.Identity.Commands.RefreshToken;

public record RefreshTokenResponse(
    string AccessToken,
    string RefreshToken,
    string UserId,
    string DisplayName,
    string Email,
    string Role);
