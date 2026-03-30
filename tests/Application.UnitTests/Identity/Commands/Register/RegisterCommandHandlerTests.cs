using Moq;
using NUnit.Framework;
using SimpleChat.Application.Common.Exceptions;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Common.Models;
using SimpleChat.Application.Identity.Commands.Register;
using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Application.UnitTests.Identity.Commands.Register;

public class RegisterCommandHandlerTests
{
    private Mock<IIdentityService> _identityService = null!;
    private Mock<IJwtTokenService> _jwtTokenService = null!;
    private Mock<ISessionService> _sessionService = null!;
    private RegisterCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _identityService = new Mock<IIdentityService>();
        _jwtTokenService = new Mock<IJwtTokenService>();
        _sessionService = new Mock<ISessionService>();

        _handler = new RegisterCommandHandler(
            _identityService.Object,
            _jwtTokenService.Object,
            _sessionService.Object);
    }

    [Test]
    public async Task Handle_FirstUser_ShouldAssignAdminRole()
    {
        // Arrange
        var command = new RegisterCommand("Admin User", "admin@test.com", "password123");
        _identityService.Setup(x => x.EmailExistsAsync("admin@test.com")).ReturnsAsync(false);
        _identityService.Setup(x => x.AnyUsersExistAsync()).ReturnsAsync(false);
        _identityService.Setup(x => x.CreateUserAsync("admin@test.com", "Admin User", "password123", UserRole.Admin))
            .ReturnsAsync((Result.Success(), "user-1"));
        _jwtTokenService.Setup(x => x.GenerateTokens("user-1", "admin@test.com", "Admin User", UserRole.Admin))
            .Returns(("access-token", "refresh-token"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.Role, Is.EqualTo("Admin"));
        Assert.That(result.AccessToken, Is.EqualTo("access-token"));
        Assert.That(result.UserId, Is.EqualTo("user-1"));
        _identityService.Verify(x => x.CreateUserAsync("admin@test.com", "Admin User", "password123", UserRole.Admin), Times.Once);
        _sessionService.Verify(x => x.StoreSessionAsync("user-1", "refresh-token", TimeSpan.FromDays(7)), Times.Once);
    }

    [Test]
    public async Task Handle_SubsequentUser_ShouldAssignMemberRole()
    {
        // Arrange
        var command = new RegisterCommand("Member User", "member@test.com", "password123");
        _identityService.Setup(x => x.EmailExistsAsync("member@test.com")).ReturnsAsync(false);
        _identityService.Setup(x => x.AnyUsersExistAsync()).ReturnsAsync(true);
        _identityService.Setup(x => x.CreateUserAsync("member@test.com", "Member User", "password123", UserRole.Member))
            .ReturnsAsync((Result.Success(), "user-2"));
        _jwtTokenService.Setup(x => x.GenerateTokens("user-2", "member@test.com", "Member User", UserRole.Member))
            .Returns(("access-token-2", "refresh-token-2"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.Role, Is.EqualTo("Member"));
        _identityService.Verify(x => x.CreateUserAsync("member@test.com", "Member User", "password123", UserRole.Member), Times.Once);
    }

    [Test]
    public void Handle_DuplicateEmail_ShouldThrowValidationException()
    {
        // Arrange
        var command = new RegisterCommand("Jane", "existing@test.com", "password123");
        _identityService.Setup(x => x.EmailExistsAsync("existing@test.com")).ReturnsAsync(true);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.That(ex!.Errors, Contains.Key("Email"));
        Assert.That(ex.Errors["Email"], Does.Contain("An account with this email already exists."));
    }

    [Test]
    public void Handle_IdentityCreateFails_ShouldThrowValidationException()
    {
        // Arrange
        var command = new RegisterCommand("Jane", "jane@test.com", "password123");
        _identityService.Setup(x => x.EmailExistsAsync("jane@test.com")).ReturnsAsync(false);
        _identityService.Setup(x => x.AnyUsersExistAsync()).ReturnsAsync(true);
        _identityService.Setup(x => x.CreateUserAsync("jane@test.com", "Jane", "password123", UserRole.Member))
            .ReturnsAsync((Result.Failure(new[] { "Password too weak" }), string.Empty));

        // Act & Assert
        Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public async Task Handle_SuccessfulRegistration_ShouldGenerateTokensAndStoreSession()
    {
        // Arrange
        var command = new RegisterCommand("Jane", "jane@test.com", "password123");
        _identityService.Setup(x => x.EmailExistsAsync("jane@test.com")).ReturnsAsync(false);
        _identityService.Setup(x => x.AnyUsersExistAsync()).ReturnsAsync(true);
        _identityService.Setup(x => x.CreateUserAsync("jane@test.com", "Jane", "password123", UserRole.Member))
            .ReturnsAsync((Result.Success(), "user-3"));
        _jwtTokenService.Setup(x => x.GenerateTokens("user-3", "jane@test.com", "Jane", UserRole.Member))
            .Returns(("jwt-token", "refresh-token"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result.AccessToken, Is.EqualTo("jwt-token"));
        Assert.That(result.RefreshToken, Is.EqualTo("refresh-token"));
        Assert.That(result.DisplayName, Is.EqualTo("Jane"));
        Assert.That(result.Email, Is.EqualTo("jane@test.com"));
        _jwtTokenService.Verify(x => x.GenerateTokens("user-3", "jane@test.com", "Jane", UserRole.Member), Times.Once);
        _sessionService.Verify(x => x.StoreSessionAsync("user-3", "refresh-token", TimeSpan.FromDays(7)), Times.Once);
    }
}
