using SimpleChat.Application.Common.Interfaces;

namespace SimpleChat.Infrastructure.Services;

public class BcryptAuthenticationProvider : IAuthenticationProvider
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
