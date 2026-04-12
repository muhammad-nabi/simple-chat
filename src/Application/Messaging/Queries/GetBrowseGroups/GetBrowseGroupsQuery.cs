using SimpleChat.Application.Common.Security;

namespace SimpleChat.Application.Messaging.Queries.GetBrowseGroups;

[Authorize]
public record GetBrowseGroupsQuery : IRequest<List<BrowseGroupDto>>;
