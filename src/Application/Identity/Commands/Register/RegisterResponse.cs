namespace SimpleChat.Application.Identity.Commands.Register;

public record RegisterResponse(
    string AccessToken,
    string RefreshToken,
    string UserId,
    string DisplayName,
    string Email,
    string Role);
