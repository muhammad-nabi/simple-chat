namespace SimpleChat.Application.Common.Interfaces;

public interface IAuthenticationProvider
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
