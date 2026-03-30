using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SimpleChat.Application.Identity.Commands.Register;
using SimpleChat.Web.Infrastructure;

namespace SimpleChat.Web.Endpoints;

public class Auth : IEndpointGroup
{
    public static string? RoutePrefix => "/api/auth";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Register, "register")
            .AllowAnonymous();
    }

    public static async Task<Ok<RegisterClientResponse>> Register(
        ISender sender,
        RegisterRequest request,
        HttpContext httpContext)
    {
        var command = new RegisterCommand(request.DisplayName, request.Email, request.Password);
        var result = await sender.Send(command);

        // Set refresh token as HttpOnly cookie
        httpContext.Response.Cookies.Append("refresh_token", result.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
        });

        var response = new RegisterClientResponse(
            result.AccessToken, result.UserId, result.DisplayName, result.Email, result.Role);

        return TypedResults.Ok(response);
    }
}

public record RegisterRequest(string DisplayName, string Email, string Password);

public record RegisterClientResponse(
    string AccessToken, string UserId, string DisplayName, string Email, string Role);
