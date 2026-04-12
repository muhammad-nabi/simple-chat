namespace SimpleChat.Application.Messaging.Commands.CreateConversation;

public class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        // Mutual exclusion: cannot supply both OtherUserId and ParticipantIds
        RuleFor(x => x.OtherUserId)
            .Null().WithMessage("Cannot specify both OtherUserId and ParticipantIds.")
            .When(x => x.ParticipantIds != null && x.ParticipantIds.Count > 0);

        // Private conversation: OtherUserId required when no ParticipantIds
        RuleFor(x => x.OtherUserId)
            .NotEmpty().WithMessage("Other user ID is required.")
            .When(x => x.ParticipantIds == null || x.ParticipantIds.Count == 0);

        // Group conversation: ParticipantIds validation
        RuleFor(x => x.ParticipantIds)
            .Must(p => p != null && p.Count >= 2 && p.Count <= 50)
            .WithMessage("Group conversations require between 2 and 50 other participants.")
            .When(x => x.ParticipantIds != null && x.ParticipantIds.Count > 0);

        RuleFor(x => x.ParticipantIds)
            .Must(p => p != null && p.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage("Participant IDs must not be empty or whitespace.")
            .When(x => x.ParticipantIds != null && x.ParticipantIds.Count > 0);

        RuleFor(x => x.ParticipantIds)
            .Must(p => p != null && p.Distinct().Count() == p.Count)
            .WithMessage("Participant IDs must not contain duplicates.")
            .When(x => x.ParticipantIds != null && x.ParticipantIds.Count > 0);

        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Group name is required for group conversations.")
            .MaximumLength(100).WithMessage("Group name must not exceed 100 characters.")
            .Must(name => name != null && name.Trim().Length > 0)
            .WithMessage("Group name must not be only whitespace.")
            .When(x => x.ParticipantIds != null && x.ParticipantIds.Count > 0);
    }
}
