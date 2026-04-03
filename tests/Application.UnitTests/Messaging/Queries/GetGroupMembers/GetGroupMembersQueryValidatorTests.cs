using FluentValidation.TestHelper;
using NUnit.Framework;
using SimpleChat.Application.Messaging.Queries.GetGroupMembers;

namespace SimpleChat.Application.UnitTests.Messaging.Queries.GetGroupMembers;

public class GetGroupMembersQueryValidatorTests
{
    private GetGroupMembersQueryValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new GetGroupMembersQueryValidator();
    }

    [Test]
    public void Validate_ValidConversationId_ShouldNotHaveErrors()
    {
        // Arrange
        GetGroupMembersQuery query = new(ConversationId: 1);

        // Act
        TestValidationResult<GetGroupMembersQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_ZeroConversationId_ShouldHaveError()
    {
        // Arrange
        GetGroupMembersQuery query = new(ConversationId: 0);

        // Act
        TestValidationResult<GetGroupMembersQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId);
    }

    [Test]
    public void Validate_NegativeConversationId_ShouldHaveError()
    {
        // Arrange
        GetGroupMembersQuery query = new(ConversationId: -5);

        // Act
        TestValidationResult<GetGroupMembersQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId);
    }
}
