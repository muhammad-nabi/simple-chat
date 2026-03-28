namespace SimpleChat.Application.Common.Models;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public bool HasMore { get; init; }
    public long? NextCursor { get; init; }
}
