namespace SimpleChat.Application.Identity.Commands.Login;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string UserId,
    string DisplayName,
    string Email,
    string Role);
