using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.Messaging.Commands.LeaveGroup;

public class LeaveGroupCommandHandler : IRequestHandler<LeaveGroupCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IUser _currentUser;
    private readonly IIdentityService _identityService;

    public LeaveGroupCommandHandler(
        IApplicationDbContext db,
        IUser currentUser,
        IIdentityService identityService)
    {
        _db = db;
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<Unit> Handle(LeaveGroupCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id
            ?? throw new UnauthorizedAccessException();

        Conversation? conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

        if (conversation == null)
        {
            throw new Common.Exceptions.NotFoundException("Conversation", request.ConversationId);
        }

        if (conversation.Type != ConversationType.Group)
        {
            throw new Common.Exceptions.ForbiddenAccessException();
        }

        ConversationParticipant? participant = await _db.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == request.ConversationId && cp.UserId == userId, cancellationToken);

        if (participant == null)
        {
            throw new Common.Exceptions.ForbiddenAccessException();
        }

        _db.ConversationParticipants.Remove(participant);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        Dictionary<string, string> displayNames = await _identityService
            .GetDisplayNamesByIdsAsync(new[] { userId }, cancellationToken);
        string displayName = displayNames.GetValueOrDefault(userId, "Unknown");

        Message systemMessage = new()
        {
            ConversationId = request.ConversationId,
            SenderId = userId,
            Content = $"{displayName} left the group",
            SentAt = now,
            MessageType = MessageType.System,
        };

        _db.Messages.Add(systemMessage);

        conversation.LastMessageAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
