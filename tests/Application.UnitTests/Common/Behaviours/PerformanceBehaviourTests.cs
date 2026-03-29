using System.Diagnostics;
using SimpleChat.Application.Common.Behaviours;
using SimpleChat.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace SimpleChat.Application.UnitTests.Common.Behaviours;

public record SlowTestRequest : IRequest<Unit>;

public class PerformanceBehaviourTests
{
    private Mock<ILogger<SlowTestRequest>> _logger = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<SlowTestRequest>>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
    }

    [Test]
    public async Task ShouldNotLogWarning_WhenRequestCompletesQuickly()
    {
        var behaviour = new PerformanceBehaviour<SlowTestRequest, Unit>(
            _logger.Object, _user.Object, _identityService.Object);

        await behaviour.Handle(
            new SlowTestRequest(),
            (_) => Task.FromResult(Unit.Value),
            CancellationToken.None);

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Test]
    public async Task ShouldNotAccumulateTime_AcrossMultipleCalls()
    {
        var behaviour = new PerformanceBehaviour<SlowTestRequest, Unit>(
            _logger.Object, _user.Object, _identityService.Object);

        // First call — fast
        await behaviour.Handle(
            new SlowTestRequest(),
            (_) => Task.FromResult(Unit.Value),
            CancellationToken.None);

        // Second call — also fast, should NOT accumulate time from first call
        await behaviour.Handle(
            new SlowTestRequest(),
            (_) => Task.FromResult(Unit.Value),
            CancellationToken.None);

        // Neither call should have triggered a warning (both are fast)
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
