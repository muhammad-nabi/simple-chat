namespace SimpleChat.Application.Messaging.Queries.GetMessageHistory;

public class GetMessageHistoryQueryValidator : AbstractValidator<GetMessageHistoryQuery>
{
    public GetMessageHistoryQueryValidator()
    {
        RuleFor(x => x.ConversationId)
            .GreaterThan(0).WithMessage("ConversationId must be greater than 0.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 200).WithMessage("Limit must be between 1 and 200.");
    }
}
