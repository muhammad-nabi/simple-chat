using Moq;
using NUnit.Framework;
using Shouldly;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Identity.Queries.GetTeamMembers;

namespace SimpleChat.Application.UnitTests.Identity.Queries.GetTeamMembers;

public class GetTeamMembersQueryHandlerTests
{
    private Mock<IIdentityService> _identityService = null!;
    private Mock<IUser> _currentUser = null!;
    private GetTeamMembersQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _identityService = new Mock<IIdentityService>();
        _currentUser = new Mock<IUser>();
        _currentUser.Setup(u => u.Id).Returns("user-1");
        _handler = new GetTeamMembersQueryHandler(_identityService.Object, _currentUser.Object);
    }

    [Test]
    public async Task Handle_ShouldReturnActiveUsersExcludingCurrentUser()
    {
        // Arrange
        GetTeamMembersQuery query = new();
        List<(string UserId, string DisplayName)> users = new()
        {
            ("user-2", "Alice"),
            ("user-3", "Bob"),
        };
        _identityService
            .Setup(s => s.GetAllActiveUsersAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        List<TeamMemberDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);
        result[0].UserId.ShouldBe("user-2");
        result[0].DisplayName.ShouldBe("Alice");
        result[1].UserId.ShouldBe("user-3");
        result[1].DisplayName.ShouldBe("Bob");
    }

    [Test]
    public async Task Handle_ShouldPassCurrentUserIdToService()
    {
        // Arrange
        GetTeamMembersQuery query = new();
        _identityService
            .Setup(s => s.GetAllActiveUsersAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string, string)>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _identityService.Verify(
            s => s.GetAllActiveUsersAsync("user-1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _currentUser.Setup(u => u.Id).Returns((string?)null);
        GetTeamMembersQueryHandler handler = new(_identityService.Object, _currentUser.Object);
        GetTeamMembersQuery query = new();

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(query, CancellationToken.None));
    }

    [Test]
    public async Task Handle_WhenNoOtherUsers_ShouldReturnEmptyList()
    {
        // Arrange
        GetTeamMembersQuery query = new();
        _identityService
            .Setup(s => s.GetAllActiveUsersAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string, string)>());

        // Act
        List<TeamMemberDto> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }
}
