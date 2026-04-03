namespace SimpleChat.Application.Messaging.Commands.LeaveGroup;

public class LeaveGroupCommandValidator : AbstractValidator<LeaveGroupCommand>
{
    public LeaveGroupCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .GreaterThan(0).WithMessage("ConversationId must be greater than 0.");
    }
}
