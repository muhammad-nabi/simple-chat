using SimpleChat.Application.Common.Models;

namespace SimpleChat.Application.Common.Interfaces;

public interface IMessageSearchService
{
    Task<PagedResult<MessageSearchResult>> SearchAsync(long userId, string term, long? cursor, int limit, CancellationToken ct = default);
}
