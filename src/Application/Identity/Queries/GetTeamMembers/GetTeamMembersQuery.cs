using SimpleChat.Application.Common.Security;

namespace SimpleChat.Application.Identity.Queries.GetTeamMembers;

[Authorize]
public record GetTeamMembersQuery : IRequest<List<TeamMemberDto>>;
