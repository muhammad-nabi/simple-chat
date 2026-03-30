using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly SigningCredentials _signingCredentials;
    private readonly int _expiryMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT_SECRET is not configured.");

        var key = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(secret));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        _expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var exp) ? exp : 30;
    }

    public (string AccessToken, string RefreshToken) GenerateTokens(
        string userId, string email, string displayName, UserRole role)
    {
        var now = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Name, displayName),
                new Claim(ClaimTypes.Role, role.ToString()),
            }),
            Expires = now.AddMinutes(_expiryMinutes),
            IssuedAt = now,
            SigningCredentials = _signingCredentials,
        };

        var handler = new JsonWebTokenHandler();
        var accessToken = handler.CreateToken(descriptor);

        var refreshTokenBytes = RandomNumberGenerator.GetBytes(32);
        var refreshToken = Convert.ToBase64String(refreshTokenBytes);

        return (accessToken, refreshToken);
    }
}
