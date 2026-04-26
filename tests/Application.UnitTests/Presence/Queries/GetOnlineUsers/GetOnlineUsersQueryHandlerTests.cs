using Moq;
using NUnit.Framework;
using Shouldly;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Presence.Models;
using SimpleChat.Application.Presence.Queries.GetOnlineUsers;
using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Application.UnitTests.Presence.Queries.GetOnlineUsers;

public class GetOnlineUsersQueryHandlerTests
{
    private Mock<ICacheService> _cache = null!;
    private GetOnlineUsersQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _cache = new Mock<ICacheService>();
        _handler = new GetOnlineUsersQueryHandler(_cache.Object);
    }

    [Test]
    public async Task Handle_OnlineUsers_ShouldReturnUsersWithStatus()
    {
        // Arrange
        HashSet<string> members = new() { "user-1", "user-2" };
        _cache.Setup(x => x.SetMembersAsync("online_users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        _cache.Setup(x => x.GetAsync<PresenceInfo>("presence:user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceInfo(PresenceStatus.Online, "Jane"));
        _cache.Setup(x => x.GetAsync<PresenceInfo>("presence:user-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceInfo(PresenceStatus.Away, "Marcus"));

        // Act
        List<OnlineUserDto> result = await _handler.Handle(new GetOnlineUsersQuery(), CancellationToken.None);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(u => u.UserId == "user-1" && u.Status == PresenceStatus.Online && u.DisplayName == "Jane");
        result.ShouldContain(u => u.UserId == "user-2" && u.Status == PresenceStatus.Away && u.DisplayName == "Marcus");
    }

    [Test]
    public async Task Handle_ExpiredUser_ShouldPruneFromSetAndExcludeFromResults()
    {
        // Arrange
        HashSet<string> members = new() { "user-1", "user-expired" };
        _cache.Setup(x => x.SetMembersAsync("online_users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        _cache.Setup(x => x.GetAsync<PresenceInfo>("presence:user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresenceInfo(PresenceStatus.Online, "Jane"));
        _cache.Setup(x => x.GetAsync<PresenceInfo>("presence:user-expired", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceInfo?)null);

        // Act
        List<OnlineUserDto> result = await _handler.Handle(new GetOnlineUsersQuery(), CancellationToken.None);

        // Assert
        result.Count.ShouldBe(1);
        result[0].UserId.ShouldBe("user-1");

        _cache.Verify(x => x.SetRemoveAsync(
            "online_users",
            "user-expired",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_EmptySet_ShouldReturnEmptyList()
    {
        // Arrange
        _cache.Setup(x => x.SetMembersAsync("online_users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());

        // Act
        List<OnlineUserDto> result = await _handler.Handle(new GetOnlineUsersQuery(), CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_AllExpired_ShouldPruneAllAndReturnEmpty()
    {
        // Arrange
        HashSet<string> members = new() { "user-a", "user-b" };
        _cache.Setup(x => x.SetMembersAsync("online_users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        _cache.Setup(x => x.GetAsync<PresenceInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PresenceInfo?)null);

        // Act
        List<OnlineUserDto> result = await _handler.Handle(new GetOnlineUsersQuery(), CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
        _cache.Verify(x => x.SetRemoveAsync("online_users", "user-a", It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(x => x.SetRemoveAsync("online_users", "user-b", It.IsAny<CancellationToken>()), Times.Once);
    }
}
