using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Identity.Commands.RefreshToken;
using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Application.UnitTests.Identity.Commands.RefreshToken;

public class RefreshTokenCommandHandlerTests
{
    private Mock<IIdentityService> _identityService = null!;
    private Mock<IJwtTokenService> _jwtTokenService = null!;
    private Mock<ISessionService> _sessionService = null!;
    private Mock<ILogger<RefreshTokenCommandHandler>> _logger = null!;
    private RefreshTokenCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _identityService = new Mock<IIdentityService>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _sessionService = new Mock<ISessionService>();
        _logger = new Mock<ILogger<RefreshTokenCommandHandler>>();

        _handler = new RefreshTokenCommandHandler(
            _identityService.Object,
            _jwtTokenService.Object,
            _sessionService.Object,
            _logger.Object);
    }

    [Test]
    public async Task Handle_ValidRefreshToken_ShouldReturnNewTokensAndUserInfo()
    {
        // Arrange
        var command = new RefreshTokenCommand("old-refresh-token");
        _sessionService.Setup(x => x.GetSessionUserIdAsync("old-refresh-token"))
            .ReturnsAsync("user-1");
        _identityService.Setup(x => x.FindUserByIdAsync("user-1"))
            .ReturnsAsync(("user-1", "Jane", "jane@test.com", UserRole.Member, true));
        _jwtTokenService.Setup(x => x.GenerateTokens("user-1", "jane@test.com", "Jane", UserRole.Member))
            .Returns(("new-access-token", "new-refresh-token"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.AccessToken, Is.EqualTo("new-access-token"));
        Assert.That(result.RefreshToken, Is.EqualTo("new-refresh-token"));
        Assert.That(result.UserId, Is.EqualTo("user-1"));
        Assert.That(result.DisplayName, Is.EqualTo("Jane"));
        Assert.That(result.Email, Is.EqualTo("jane@test.com"));
        Assert.That(result.Role, Is.EqualTo("Member"));
    }

    [Test]
    public async Task Handle_ValidRefreshToken_ShouldRotateTokens()
    {
        // Arrange
        var command = new RefreshTokenCommand("old-refresh-token");
        _sessionService.Setup(x => x.GetSessionUserIdAsync("old-refresh-token"))
            .ReturnsAsync("user-1");
        _identityService.Setup(x => x.FindUserByIdAsync("user-1"))
            .ReturnsAsync(("user-1", "Jane", "jane@test.com", UserRole.Member, true));
        _jwtTokenService.Setup(x => x.GenerateTokens("user-1", "jane@test.com", "Jane", UserRole.Member))
            .Returns(("new-access-token", "new-refresh-token"));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — old token marked as used, invalidated, new session stored
        _sessionService.Verify(x => x.MarkTokenAsUsedAsync("old-refresh-token", "user-1", TimeSpan.FromDays(7)), Times.Once);
        _sessionService.Verify(x => x.InvalidateSessionAsync("old-refresh-token"), Times.Once);
        _sessionService.Verify(x => x.StoreSessionAsync("user-1", "new-refresh-token", TimeSpan.FromDays(7)), Times.Once);
    }

    [Test]
    public void Handle_ExpiredOrInvalidToken_ShouldThrowUnauthorized()
    {
        // Arrange
        var command = new RefreshTokenCommand("expired-token");
        _sessionService.Setup(x => x.GetSessionUserIdAsync("expired-token"))
            .ReturnsAsync((string?)null);
        _sessionService.Setup(x => x.GetUsedTokenUserIdAsync("expired-token"))
            .ReturnsAsync((string?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Invalid or expired refresh token."));
    }

    [Test]
    public void Handle_ReusedToken_ShouldInvalidateAllSessionsAndThrow()
    {
        // Arrange — token not in active sessions but found in used-token tracking
        var command = new RefreshTokenCommand("reused-token");
        _sessionService.Setup(x => x.GetSessionUserIdAsync("reused-token"))
            .ReturnsAsync((string?)null);
        _sessionService.Setup(x => x.GetUsedTokenUserIdAsync("reused-token"))
            .ReturnsAsync("user-1");

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Invalid or expired refresh token."));

        // Verify emergency lockout — all sessions invalidated
        _sessionService.Verify(x => x.InvalidateAllSessionsAsync("user-1"), Times.Once);
    }

    [Test]
    public void Handle_DeactivatedUser_ShouldInvalidateSessionAndThrow()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-token");
        _sessionService.Setup(x => x.GetSessionUserIdAsync("valid-token"))
            .ReturnsAsync("user-1");
        _identityService.Setup(x => x.FindUserByIdAsync("user-1"))
            .ReturnsAsync(("user-1", "Jane", "jane@test.com", UserRole.Member, false));

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Invalid or expired refresh token."));

        // Verify session was invalidated
        _sessionService.Verify(x => x.InvalidateSessionAsync("valid-token"), Times.Once);

        // Verify no new tokens were generated
        _jwtTokenService.Verify(x => x.GenerateTokens(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<UserRole>()), Times.Never);
    }

    [Test]
    public void Handle_UserNotFound_ShouldInvalidateSessionAndThrow()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-token");
        _sessionService.Setup(x => x.GetSessionUserIdAsync("valid-token"))
            .ReturnsAsync("deleted-user-id");
        _identityService.Setup(x => x.FindUserByIdAsync("deleted-user-id"))
            .ReturnsAsync(((string, string, string, UserRole, bool)?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Invalid or expired refresh token."));

        // Verify session was invalidated
        _sessionService.Verify(x => x.InvalidateSessionAsync("valid-token"), Times.Once);
    }
}
