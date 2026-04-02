using FluentValidation.TestHelper;
using NUnit.Framework;
using SimpleChat.Application.Messaging.Commands.SendMessage;

namespace SimpleChat.Application.UnitTests.Messaging.Commands.SendMessage;

public class SendMessageCommandValidatorTests
{
    private SendMessageCommandValidator _validator = null!;

    [SetUp]
    public void Setup()
    {
        _validator = new SendMessageCommandValidator();
    }

    [Test]
    public async Task ShouldPassValidation_WhenAllFieldsValid()
    {
        SendMessageCommand command = new(1, "Hello world");
        TestValidationResult<SendMessageCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task ShouldFailValidation_WhenContentEmpty()
    {
        SendMessageCommand command = new(1, "");
        TestValidationResult<SendMessageCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Message content is required.");
    }

    [Test]
    public async Task ShouldFailValidation_WhenContentExceeds4000Characters()
    {
        string longContent = new('a', 4001);
        SendMessageCommand command = new(1, longContent);
        TestValidationResult<SendMessageCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Message content must not exceed 4000 characters.");
    }

    [Test]
    public async Task ShouldPassValidation_WhenContentExactly4000Characters()
    {
        string maxContent = new('a', 4000);
        SendMessageCommand command = new(1, maxContent);
        TestValidationResult<SendMessageCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }

    [Test]
    public async Task ShouldFailValidation_WhenConversationIdIsZero()
    {
        SendMessageCommand command = new(0, "Hello");
        TestValidationResult<SendMessageCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ConversationId)
            .WithErrorMessage("ConversationId must be greater than 0.");
    }

    [Test]
    public async Task ShouldFailValidation_WhenConversationIdIsNegative()
    {
        SendMessageCommand command = new(-1, "Hello");
        TestValidationResult<SendMessageCommand> result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.ConversationId);
    }
}
