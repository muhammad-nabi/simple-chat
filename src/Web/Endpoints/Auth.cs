using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SimpleChat.Application.Identity.Commands.Login;
using SimpleChat.Application.Identity.Commands.Register;
using SimpleChat.Web.Infrastructure;

namespace SimpleChat.Web.Endpoints;

public class Auth : IEndpointGroup
{
    public static string? RoutePrefix => "/api/auth";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Register, "register")
            .AllowAnonymous()
            .RequireRateLimiting("auth");

        groupBuilder.MapPost(Login, "login")
            .AllowAnonymous()
            .RequireRateLimiting("auth-per-user");
    }

    public static async Task<Ok<RegisterClientResponse>> Register(
        ISender sender,
        RegisterRequest request,
        HttpContext httpContext)
    {
        var command = new RegisterCommand(request.DisplayName, request.Email, request.Password);
        var result = await sender.Send(command);

        SetRefreshTokenCookie(httpContext, result.RefreshToken);

        var response = new RegisterClientResponse(
            result.AccessToken, result.UserId, result.DisplayName, result.Email, result.Role);

        return TypedResults.Ok(response);
    }

    public static async Task<Ok<LoginClientResponse>> Login(
        ISender sender,
        LoginRequest request,
        HttpContext httpContext)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await sender.Send(command);

        SetRefreshTokenCookie(httpContext, result.RefreshToken);

        var response = new LoginClientResponse(
            result.AccessToken, result.UserId, result.DisplayName, result.Email, result.Role);

        return TypedResults.Ok(response);
    }

    private static void SetRefreshTokenCookie(HttpContext httpContext, string refreshToken)
    {
        httpContext.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
        });
    }
}

public record RegisterRequest(string DisplayName, string Email, string Password);

public record RegisterClientResponse(
    string AccessToken, string UserId, string DisplayName, string Email, string Role);

public record LoginRequest(string Email, string Password);

public record LoginClientResponse(
    string AccessToken, string UserId, string DisplayName, string Email, string Role);
