using Moq;
using NUnit.Framework;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Identity.Commands.Logout;

namespace SimpleChat.Application.UnitTests.Identity.Commands.Logout;

public class LogoutCommandHandlerTests
{
    private Mock<ISessionService> _sessionService = null!;
    private LogoutCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _sessionService = new Mock<ISessionService>();
        _handler = new LogoutCommandHandler(_sessionService.Object);
    }

    [Test]
    public async Task Handle_ValidRefreshToken_ShouldInvalidateSession()
    {
        // Arrange
        var command = new LogoutCommand("active-refresh-token");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — only the specific session is invalidated (single-device logout)
        _sessionService.Verify(x => x.InvalidateSessionAsync("active-refresh-token"), Times.Once);
        _sessionService.Verify(x => x.InvalidateAllSessionsAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Handle_NonExistentToken_ShouldCompleteWithoutError()
    {
        // Arrange — InvalidateSessionAsync is a no-op if token doesn't exist
        var command = new LogoutCommand("already-invalidated-token");

        // Act & Assert — should not throw
        await _handler.Handle(command, CancellationToken.None);

        _sessionService.Verify(x => x.InvalidateSessionAsync("already-invalidated-token"), Times.Once);
    }
}
