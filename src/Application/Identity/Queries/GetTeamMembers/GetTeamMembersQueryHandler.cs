using SimpleChat.Application.Common.Interfaces;

namespace SimpleChat.Application.Identity.Queries.GetTeamMembers;

public class GetTeamMembersQueryHandler : IRequestHandler<GetTeamMembersQuery, List<TeamMemberDto>>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _currentUser;

    public GetTeamMembersQueryHandler(IIdentityService identityService, IUser currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task<List<TeamMemberDto>> Handle(GetTeamMembersQuery request, CancellationToken cancellationToken)
    {
        string currentUserId = _currentUser.Id
            ?? throw new UnauthorizedAccessException();

        List<(string UserId, string DisplayName)> users =
            await _identityService.GetAllActiveUsersAsync(currentUserId, cancellationToken);

        return users
            .Select(u => new TeamMemberDto(u.UserId, u.DisplayName))
            .ToList();
    }
}
