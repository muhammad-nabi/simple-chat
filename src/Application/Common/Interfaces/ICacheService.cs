namespace SimpleChat.Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<IReadOnlySet<string>> SetMembersAsync(string key, CancellationToken ct = default);
}
