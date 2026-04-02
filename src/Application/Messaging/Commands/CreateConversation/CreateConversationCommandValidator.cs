namespace SimpleChat.Application.Messaging.Commands.CreateConversation;

public class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        RuleFor(x => x.OtherUserId)
            .NotEmpty().WithMessage("Other user ID is required.");
    }
}
