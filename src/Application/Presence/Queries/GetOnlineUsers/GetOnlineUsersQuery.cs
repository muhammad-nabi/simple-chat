using SimpleChat.Application.Common.Security;

namespace SimpleChat.Application.Presence.Queries.GetOnlineUsers;

[Authorize]
public record GetOnlineUsersQuery : IRequest<List<OnlineUserDto>>;
