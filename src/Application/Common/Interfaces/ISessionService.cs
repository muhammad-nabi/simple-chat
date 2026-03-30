namespace SimpleChat.Application.Common.Interfaces;

public interface ISessionService
{
    Task StoreSessionAsync(string userId, string refreshToken, TimeSpan expiry);
    Task<string?> GetSessionUserIdAsync(string refreshToken);
    Task InvalidateSessionAsync(string refreshToken);
    Task InvalidateAllSessionsAsync(string userId);
}
