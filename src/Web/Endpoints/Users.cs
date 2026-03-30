using Microsoft.AspNetCore.Http.HttpResults;

namespace SimpleChat.Web.Endpoints;

public class Users : IEndpointGroup
{
    public static string? RoutePrefix => "/api/users";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        // Auth endpoints moved to Auth endpoint group (JWT-based)
        // User management endpoints will be added in Epic 8
    }
}
