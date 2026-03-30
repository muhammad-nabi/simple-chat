using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Application.Common.Interfaces;

public interface IJwtTokenService
{
    (string AccessToken, string RefreshToken) GenerateTokens(
        string userId, string email, string displayName, UserRole role);
}
