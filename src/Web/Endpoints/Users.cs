using Microsoft.AspNetCore.Http.HttpResults;
using SimpleChat.Application.Identity.Queries.GetTeamMembers;

namespace SimpleChat.Web.Endpoints;

public class Users : IEndpointGroup
{
    public static string? RoutePrefix => "/api/users";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetTeamMembers, "team-members")
            .RequireAuthorization();
    }

    public static async Task<Ok<List<TeamMemberDto>>> GetTeamMembers(
        ISender sender)
    {
        List<TeamMemberDto> members = await sender.Send(new GetTeamMembersQuery());
        return TypedResults.Ok(members);
    }
}
