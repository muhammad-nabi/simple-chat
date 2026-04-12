using FluentValidation.TestHelper;
using NUnit.Framework;
using SimpleChat.Application.Messaging.Commands.JoinGroup;

namespace SimpleChat.Application.UnitTests.Messaging.Commands.JoinGroup;

public class JoinGroupCommandValidatorTests
{
    private JoinGroupCommandValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new JoinGroupCommandValidator();
    }

    [Test]
    public void Validate_ValidConversationId_ShouldNotHaveErrors()
    {
        // Arrange
        JoinGroupCommand command = new(ConversationId: 1);

        // Act
        TestValidationResult<JoinGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_ZeroConversationId_ShouldHaveError()
    {
        // Arrange
        JoinGroupCommand command = new(ConversationId: 0);

        // Act
        TestValidationResult<JoinGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId);
    }

    [Test]
    public void Validate_NegativeConversationId_ShouldHaveError()
    {
        // Arrange
        JoinGroupCommand command = new(ConversationId: -5);

        // Act
        TestValidationResult<JoinGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId);
    }
}
