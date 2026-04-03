using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.Messaging.Commands.InviteToGroup;

public class InviteToGroupCommandHandler : IRequestHandler<InviteToGroupCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IUser _currentUser;
    private readonly IIdentityService _identityService;

    public InviteToGroupCommandHandler(
        IApplicationDbContext db,
        IUser currentUser,
        IIdentityService identityService)
    {
        _db = db;
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<Unit> Handle(InviteToGroupCommand request, CancellationToken cancellationToken)
    {
        string inviterId = _currentUser.Id
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

        bool isParticipant = await _db.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == request.ConversationId && cp.UserId == inviterId, cancellationToken);

        if (!isParticipant)
        {
            throw new Common.Exceptions.ForbiddenAccessException();
        }

        // Validate all invited users exist
        foreach (string userId in request.UserIds)
        {
            bool exists = await _identityService.UserExistsAsync(userId, cancellationToken);
            if (!exists)
            {
                throw new Common.Exceptions.NotFoundException("User", userId);
            }
        }

        // Filter out users who are already participants
        List<string> existingParticipantIds = await _db.ConversationParticipants
            .Where(cp => cp.ConversationId == request.ConversationId && request.UserIds.Contains(cp.UserId))
            .Select(cp => cp.UserId)
            .ToListAsync(cancellationToken);

        List<string> newUserIds = request.UserIds
            .Where(id => !existingParticipantIds.Contains(id))
            .ToList();

        if (newUserIds.Count == 0)
        {
            return Unit.Value;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Resolve display names for inviter + all new users
        List<string> allUserIds = new(newUserIds) { inviterId };
        Dictionary<string, string> displayNames = await _identityService
            .GetDisplayNamesByIdsAsync(allUserIds, cancellationToken);

        string inviterName = displayNames.GetValueOrDefault(inviterId, "Unknown");

        foreach (string userId in newUserIds)
        {
            ConversationParticipant participant = new()
            {
                ConversationId = request.ConversationId,
                UserId = userId,
                JoinedAt = now,
            };
            _db.ConversationParticipants.Add(participant);

            string userName = displayNames.GetValueOrDefault(userId, "Unknown");
            Message systemMessage = new()
            {
                ConversationId = request.ConversationId,
                SenderId = inviterId,
                Content = $"{inviterName} added {userName}",
                SentAt = now,
                MessageType = MessageType.System,
            };
            _db.Messages.Add(systemMessage);
        }

        conversation.LastMessageAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
