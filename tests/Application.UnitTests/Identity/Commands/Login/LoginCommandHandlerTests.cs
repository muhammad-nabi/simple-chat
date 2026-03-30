using Moq;
using NUnit.Framework;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Identity.Commands.Login;
using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Application.UnitTests.Identity.Commands.Login;

public class LoginCommandHandlerTests
{
    private Mock<IIdentityService> _identityService = null!;
    private Mock<IJwtTokenService> _jwtTokenService = null!;
    private Mock<ISessionService> _sessionService = null!;
    private LoginCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _identityService = new Mock<IIdentityService>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _sessionService = new Mock<ISessionService>();

        _handler = new LoginCommandHandler(
            _identityService.Object,
            _jwtTokenService.Object,
            _sessionService.Object);
    }

    [Test]
    public async Task Handle_ValidCredentials_ShouldReturnTokensAndUserInfo()
    {
        // Arrange
        var command = new LoginCommand("jane@test.com", "password123");
        _identityService.Setup(x => x.FindUserByEmailAsync("jane@test.com"))
            .ReturnsAsync(("user-1", "Jane", "jane@test.com", UserRole.Member, true));
        _identityService.Setup(x => x.CheckPasswordAsync("user-1", "password123"))
            .ReturnsAsync(true);
        _jwtTokenService.Setup(x => x.GenerateTokens("user-1", "jane@test.com", "Jane", UserRole.Member))
            .Returns(("access-token", "refresh-token"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.AccessToken, Is.EqualTo("access-token"));
        Assert.That(result.RefreshToken, Is.EqualTo("refresh-token"));
        Assert.That(result.UserId, Is.EqualTo("user-1"));
        Assert.That(result.DisplayName, Is.EqualTo("Jane"));
        Assert.That(result.Email, Is.EqualTo("jane@test.com"));
        Assert.That(result.Role, Is.EqualTo("Member"));
    }

    [Test]
    public async Task Handle_ValidCredentials_ShouldStoreSessionInRedis()
    {
        // Arrange
        var command = new LoginCommand("jane@test.com", "password123");
        _identityService.Setup(x => x.FindUserByEmailAsync("jane@test.com"))
            .ReturnsAsync(("user-1", "Jane", "jane@test.com", UserRole.Member, true));
        _identityService.Setup(x => x.CheckPasswordAsync("user-1", "password123"))
            .ReturnsAsync(true);
        _jwtTokenService.Setup(x => x.GenerateTokens("user-1", "jane@test.com", "Jane", UserRole.Member))
            .Returns(("access-token", "refresh-token"));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _sessionService.Verify(x => x.StoreSessionAsync("user-1", "refresh-token", TimeSpan.FromDays(7)), Times.Once);
    }

    [Test]
    public void Handle_UserNotFound_ShouldThrowUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("unknown@test.com", "password123");
        _identityService.Setup(x => x.FindUserByEmailAsync("unknown@test.com"))
            .ReturnsAsync((ValueTuple<string, string, string, UserRole, bool>?)null);
        _identityService.Setup(x => x.VerifyDummyPasswordAsync("password123"))
            .Returns(Task.CompletedTask);

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Invalid email or password."));

        // Verify dummy password check was called for timing-attack mitigation
        _identityService.Verify(x => x.VerifyDummyPasswordAsync("password123"), Times.Once);
    }

    [Test]
    public void Handle_WrongPassword_ShouldThrowUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("jane@test.com", "wrongpassword");
        _identityService.Setup(x => x.FindUserByEmailAsync("jane@test.com"))
            .ReturnsAsync(("user-1", "Jane", "jane@test.com", UserRole.Member, true));
        _identityService.Setup(x => x.CheckPasswordAsync("user-1", "wrongpassword"))
            .ReturnsAsync(false);

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Invalid email or password."));
    }

    [Test]
    public void Handle_DeactivatedUser_ShouldThrowUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("jane@test.com", "password123");
        _identityService.Setup(x => x.FindUserByEmailAsync("jane@test.com"))
            .ReturnsAsync(("user-1", "Jane", "jane@test.com", UserRole.Member, false));
        _identityService.Setup(x => x.CheckPasswordAsync("user-1", "password123"))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.Message, Is.EqualTo("Invalid email or password."));

        // Verify no tokens were generated
        _jwtTokenService.Verify(x => x.GenerateTokens(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<UserRole>()), Times.Never);
        _sessionService.Verify(x => x.StoreSessionAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>()), Times.Never);
    }

    [Test]
    public async Task Handle_AdminUser_ShouldReturnAdminRole()
    {
        // Arrange
        var command = new LoginCommand("admin@test.com", "password123");
        _identityService.Setup(x => x.FindUserByEmailAsync("admin@test.com"))
            .ReturnsAsync(("user-1", "Admin", "admin@test.com", UserRole.Admin, true));
        _identityService.Setup(x => x.CheckPasswordAsync("user-1", "password123"))
            .ReturnsAsync(true);
        _jwtTokenService.Setup(x => x.GenerateTokens("user-1", "admin@test.com", "Admin", UserRole.Admin))
            .Returns(("admin-token", "admin-refresh"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.Role, Is.EqualTo("Admin"));
    }
}
