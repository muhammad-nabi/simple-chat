namespace SimpleChat.Application.Messaging.Commands.InviteToGroup;

public class InviteToGroupCommandValidator : AbstractValidator<InviteToGroupCommand>
{
    public InviteToGroupCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .GreaterThan(0).WithMessage("ConversationId must be greater than 0.");

        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("At least one user must be invited.");

        RuleFor(x => x.UserIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate user IDs are not allowed.");
    }
}
