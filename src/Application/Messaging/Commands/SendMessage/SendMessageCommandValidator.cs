namespace SimpleChat.Application.Messaging.Commands.SendMessage;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .GreaterThan(0).WithMessage("ConversationId must be greater than 0.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required.")
            .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Message content cannot be only whitespace.")
            .MaximumLength(4000).WithMessage("Message content must not exceed 4000 characters.");
    }
}
