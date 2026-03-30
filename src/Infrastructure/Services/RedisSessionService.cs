using SimpleChat.Application.Common.Interfaces;
using StackExchange.Redis;

namespace SimpleChat.Infrastructure.Services;

public class RedisSessionService : ISessionService
{
    private readonly IConnectionMultiplexer _redis;
    private const string SessionPrefix = "session:";
    private const string UserSessionsPrefix = "user-sessions:";

    public RedisSessionService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task StoreSessionAsync(string userId, string refreshToken, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        var sessionKey = SessionPrefix + refreshToken;
        var userSessionsKey = UserSessionsPrefix + userId;

        await db.StringSetAsync(sessionKey, userId, expiry);
        await db.SetAddAsync(userSessionsKey, refreshToken);
        await db.KeyExpireAsync(userSessionsKey, expiry);
    }

    public async Task<string?> GetSessionUserIdAsync(string refreshToken)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(SessionPrefix + refreshToken);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task InvalidateSessionAsync(string refreshToken)
    {
        var db = _redis.GetDatabase();
        var userId = await db.StringGetAsync(SessionPrefix + refreshToken);
        await db.KeyDeleteAsync(SessionPrefix + refreshToken);

        if (userId.HasValue)
        {
            await db.SetRemoveAsync(UserSessionsPrefix + userId, refreshToken);
        }
    }

    public async Task InvalidateAllSessionsAsync(string userId)
    {
        var db = _redis.GetDatabase();
        var userSessionsKey = UserSessionsPrefix + userId;
        var tokens = await db.SetMembersAsync(userSessionsKey);

        foreach (var token in tokens)
        {
            await db.KeyDeleteAsync(SessionPrefix + token);
        }

        await db.KeyDeleteAsync(userSessionsKey);
    }
}
