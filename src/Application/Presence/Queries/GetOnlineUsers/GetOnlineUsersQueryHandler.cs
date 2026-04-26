using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Presence.Models;

namespace SimpleChat.Application.Presence.Queries.GetOnlineUsers;

public class GetOnlineUsersQueryHandler : IRequestHandler<GetOnlineUsersQuery, List<OnlineUserDto>>
{
    private readonly ICacheService _cache;

    private const string PresenceKeyPrefix = "presence:";
    private const string OnlineUsersKey = "online_users";

    public GetOnlineUsersQueryHandler(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<List<OnlineUserDto>> Handle(GetOnlineUsersQuery request, CancellationToken cancellationToken)
    {
        IReadOnlySet<string> memberIds = await _cache.SetMembersAsync(OnlineUsersKey, cancellationToken);
        List<OnlineUserDto> results = new();

        foreach (string userId in memberIds)
        {
            PresenceInfo? info = await _cache.GetAsync<PresenceInfo>(PresenceKeyPrefix + userId, cancellationToken);

            if (info != null)
            {
                results.Add(new OnlineUserDto(userId, info.DisplayName, info.Status));
            }
            else
            {
                // Lazy prune: TTL key expired, remove from Set
                await _cache.SetRemoveAsync(OnlineUsersKey, userId, cancellationToken);
            }
        }

        return results;
    }
}
