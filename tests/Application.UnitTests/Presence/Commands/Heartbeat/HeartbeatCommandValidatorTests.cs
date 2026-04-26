using FluentValidation.TestHelper;
using NUnit.Framework;
using SimpleChat.Application.Presence.Commands.Heartbeat;
using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Application.UnitTests.Presence.Commands.Heartbeat;

public class HeartbeatCommandValidatorTests
{
    private HeartbeatCommandValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new HeartbeatCommandValidator();
    }

    [Test]
    public void Validate_OnlineStatus_ShouldPass()
    {
        // Arrange
        HeartbeatCommand command = new(PresenceStatus.Online);

        // Act
        TestValidationResult<HeartbeatCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_AwayStatus_ShouldPass()
    {
        // Arrange
        HeartbeatCommand command = new(PresenceStatus.Away);

        // Act
        TestValidationResult<HeartbeatCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_OfflineStatus_ShouldFail()
    {
        // Arrange
        HeartbeatCommand command = new(PresenceStatus.Offline);

        // Act
        TestValidationResult<HeartbeatCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Test]
    public void Validate_InvalidEnumValue_ShouldFail()
    {
        // Arrange
        HeartbeatCommand command = new((PresenceStatus)99);

        // Act
        TestValidationResult<HeartbeatCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
