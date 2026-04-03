namespace SimpleChat.Application.Messaging.Commands.JoinGroup;

public class JoinGroupCommandValidator : AbstractValidator<JoinGroupCommand>
{
    public JoinGroupCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .GreaterThan(0).WithMessage("ConversationId must be greater than 0.");
    }
}
