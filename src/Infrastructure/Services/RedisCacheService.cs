using System.Text.Json;
using Microsoft.Extensions.Logging;
using SimpleChat.Application.Common.Interfaces;
using StackExchange.Redis;

namespace SimpleChat.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue value = await db.StringGetAsync(key);

        if (!value.HasValue)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Corrupt payload at Redis key {Key}; treating as missing.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        string json = JsonSerializer.Serialize(value);

        if (expiry.HasValue)
        {
            await db.StringSetAsync(key, json, expiry.Value);
        }
        else
        {
            await db.StringSetAsync(key, json);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
    }

    public async Task<IReadOnlySet<string>> SetMembersAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        RedisValue[] members = await db.SetMembersAsync(key);
        HashSet<string> result = new();

        foreach (RedisValue member in members)
        {
            if (member.HasValue)
            {
                result.Add(member.ToString());
            }
        }

        return result;
    }

    public async Task SetAddAsync(string key, string member, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        await db.SetAddAsync(key, member);
    }

    public async Task SetRemoveAsync(string key, string member, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        await db.SetRemoveAsync(key, member);
    }

    public async Task<bool> KeyExistsAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        IDatabase db = _redis.GetDatabase();
        return await db.KeyExistsAsync(key);
    }
}
