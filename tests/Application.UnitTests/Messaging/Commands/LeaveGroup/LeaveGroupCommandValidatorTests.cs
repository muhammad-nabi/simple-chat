using FluentValidation.TestHelper;
using NUnit.Framework;
using SimpleChat.Application.Messaging.Commands.LeaveGroup;

namespace SimpleChat.Application.UnitTests.Messaging.Commands.LeaveGroup;

public class LeaveGroupCommandValidatorTests
{
    private LeaveGroupCommandValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new LeaveGroupCommandValidator();
    }

    [Test]
    public void Validate_ValidConversationId_ShouldNotHaveErrors()
    {
        // Arrange
        LeaveGroupCommand command = new(ConversationId: 1);

        // Act
        TestValidationResult<LeaveGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_ZeroConversationId_ShouldHaveError()
    {
        // Arrange
        LeaveGroupCommand command = new(ConversationId: 0);

        // Act
        TestValidationResult<LeaveGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId);
    }

    [Test]
    public void Validate_NegativeConversationId_ShouldHaveError()
    {
        // Arrange
        LeaveGroupCommand command = new(ConversationId: -5);

        // Act
        TestValidationResult<LeaveGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId);
    }
}
