using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SimpleChat.Application.Identity.Commands.Login;
using SimpleChat.Application.Identity.Commands.RefreshToken;
using SimpleChat.Application.Identity.Commands.Register;
using SimpleChat.Application.Identity.Constants;
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

        groupBuilder.MapPost(Refresh, "refresh")
            .AllowAnonymous()
            .RequireRateLimiting("auth");
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

    public static async Task<IResult> Refresh(
        ISender sender,
        HttpContext httpContext)
    {
        var refreshToken = httpContext.Request.Cookies["refresh_token"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            var command = new RefreshTokenCommand(refreshToken);
            var result = await sender.Send(command);

            SetRefreshTokenCookie(httpContext, result.RefreshToken);

            var response = new RefreshClientResponse(
                result.AccessToken, result.UserId, result.DisplayName, result.Email, result.Role);

            return TypedResults.Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            ClearRefreshTokenCookie(httpContext);
            return TypedResults.Unauthorized();
        }
        catch (Exception)
        {
            ClearRefreshTokenCookie(httpContext);
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static void SetRefreshTokenCookie(HttpContext httpContext, string refreshToken)
    {
        httpContext.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(TokenConstants.RefreshTokenExpiryDays),
        });
    }

    private static void ClearRefreshTokenCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Append("refresh_token", "", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
        });
    }
}

public record RegisterRequest(string DisplayName, string Email, string Password);

public record RegisterClientResponse(
    string AccessToken, string UserId, string DisplayName, string Email, string Role);

public record LoginRequest(string Email, string Password);

public record LoginClientResponse(
    string AccessToken, string UserId, string DisplayName, string Email, string Role);

public record RefreshClientResponse(
    string AccessToken, string UserId, string DisplayName, string Email, string Role);
