using FluentValidation.TestHelper;
using NUnit.Framework;
using SimpleChat.Application.Messaging.Commands.InviteToGroup;

namespace SimpleChat.Application.UnitTests.Messaging.Commands.InviteToGroup;

public class InviteToGroupCommandValidatorTests
{
    private InviteToGroupCommandValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new InviteToGroupCommandValidator();
    }

    [Test]
    public void Validate_ValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        InviteToGroupCommand command = new(ConversationId: 1, UserIds: new List<string> { "user-1" });

        // Act
        TestValidationResult<InviteToGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_ZeroConversationId_ShouldHaveError()
    {
        // Arrange
        InviteToGroupCommand command = new(ConversationId: 0, UserIds: new List<string> { "user-1" });

        // Act
        TestValidationResult<InviteToGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId);
    }

    [Test]
    public void Validate_EmptyUserIds_ShouldHaveError()
    {
        // Arrange
        InviteToGroupCommand command = new(ConversationId: 1, UserIds: new List<string>());

        // Act
        TestValidationResult<InviteToGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserIds);
    }

    [Test]
    public void Validate_DuplicateUserIds_ShouldHaveError()
    {
        // Arrange
        InviteToGroupCommand command = new(ConversationId: 1, UserIds: new List<string> { "user-1", "user-1" });

        // Act
        TestValidationResult<InviteToGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserIds);
    }

    [Test]
    public void Validate_MultipleUniqueUserIds_ShouldNotHaveErrors()
    {
        // Arrange
        InviteToGroupCommand command = new(ConversationId: 1, UserIds: new List<string> { "user-1", "user-2" });

        // Act
        TestValidationResult<InviteToGroupCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
