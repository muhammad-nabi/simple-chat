using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Presence.Commands.Heartbeat;
using SimpleChat.Application.Presence.Models;
using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Application.UnitTests.Presence.Commands.Heartbeat;

public class HeartbeatCommandHandlerTests
{
    private Mock<IUser> _currentUser = null!;
    private Mock<IIdentityService> _identityService = null!;
    private Mock<ICacheService> _cache = null!;
    private Mock<ILogger<HeartbeatCommandHandler>> _logger = null!;
    private HeartbeatCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _currentUser = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
        _cache = new Mock<ICacheService>();
        _logger = new Mock<ILogger<HeartbeatCommandHandler>>();

        _handler = new HeartbeatCommandHandler(
            _currentUser.Object,
            _identityService.Object,
            _cache.Object,
            _logger.Object);
    }

    [Test]
    public async Task Handle_OnlineHeartbeat_ShouldSetPresenceKeyWith90sTtlAndAddToSet()
    {
        // Arrange
        _currentUser.Setup(x => x.Id).Returns("user-1");
        _identityService.Setup(x => x.GetDisplayNamesByIdsAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { { "user-1", "Jane" } });

        HeartbeatCommand command = new(PresenceStatus.Online);

        // Act
        Unit result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBe(Unit.Value);

        _cache.Verify(x => x.SetAsync(
            "presence:user-1",
            It.Is<PresenceInfo>(p => p.Status == PresenceStatus.Online && p.DisplayName == "Jane"),
            TimeSpan.FromSeconds(90),
            It.IsAny<CancellationToken>()), Times.Once);

        _cache.Verify(x => x.SetAddAsync(
            "online_users",
            "user-1",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_AwayHeartbeat_ShouldSetPresenceKeyWithAwayStatus()
    {
        // Arrange
        _currentUser.Setup(x => x.Id).Returns("user-2");
        _identityService.Setup(x => x.GetDisplayNamesByIdsAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string> { { "user-2", "Marcus" } });

        HeartbeatCommand command = new(PresenceStatus.Away);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _cache.Verify(x => x.SetAsync(
            "presence:user-2",
            It.Is<PresenceInfo>(p => p.Status == PresenceStatus.Away && p.DisplayName == "Marcus"),
            TimeSpan.FromSeconds(90),
            It.IsAny<CancellationToken>()), Times.Once);

        _cache.Verify(x => x.SetAddAsync(
            "online_users",
            "user-2",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_NoAuthenticatedUser_ShouldThrowArgumentException()
    {
        // Arrange
        _currentUser.Setup(x => x.Id).Returns((string?)null);
        HeartbeatCommand command = new(PresenceStatus.Online);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}
