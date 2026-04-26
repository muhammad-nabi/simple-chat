using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SimpleChat.Application.Presence.Commands.Heartbeat;
using SimpleChat.Application.Presence.Queries.GetOnlineUsers;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Web.Infrastructure;

namespace SimpleChat.Web.Endpoints;

public class Presence : IEndpointGroup
{
    public static string? RoutePrefix => "/api/presence";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Heartbeat, "heartbeat")
            .RequireAuthorization();

        groupBuilder.MapGet(GetOnlineUsers, "online")
            .RequireAuthorization();
    }

    public static async Task<Results<Ok, BadRequest<string>>> Heartbeat(ISender sender, HeartbeatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status)
            || !Enum.TryParse(request.Status, ignoreCase: true, out PresenceStatus status))
        {
            return TypedResults.BadRequest($"Invalid status '{request.Status}'. Expected 'Online' or 'Away'.");
        }

        await sender.Send(new HeartbeatCommand(status));
        return TypedResults.Ok();
    }

    public static async Task<Ok<List<OnlineUserDto>>> GetOnlineUsers(ISender sender)
    {
        List<OnlineUserDto> result = await sender.Send(new GetOnlineUsersQuery());
        return TypedResults.Ok(result);
    }
}

public record HeartbeatRequest(string? Status);
