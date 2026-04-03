using SimpleChat.Application.Common.Exceptions;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.Messaging.Commands.JoinGroup;

public class JoinGroupCommandHandler : IRequestHandler<JoinGroupCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly IUser _currentUser;
    private readonly IIdentityService _identityService;

    public JoinGroupCommandHandler(
        IApplicationDbContext db,
        IUser currentUser,
        IIdentityService identityService)
    {
        _db = db;
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<Unit> Handle(JoinGroupCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id
            ?? throw new UnauthorizedAccessException();

        // Load conversation
        Conversation? conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

        if (conversation == null)
        {
            throw new Common.Exceptions.NotFoundException("Conversation", request.ConversationId);
        }

        // Must be a Group conversation
        if (conversation.Type != ConversationType.Group)
        {
            throw new ForbiddenAccessException();
        }

        // Check if user is already a participant
        bool alreadyParticipant = await _db.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == request.ConversationId && cp.UserId == userId, cancellationToken);

        if (alreadyParticipant)
        {
            throw new ForbiddenAccessException();
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Add user as participant
        ConversationParticipant participant = new()
        {
            ConversationId = request.ConversationId,
            UserId = userId,
            JoinedAt = now,
        };

        _db.ConversationParticipants.Add(participant);

        // Resolve display name for system message
        Dictionary<string, string> displayNames = await _identityService
            .GetDisplayNamesByIdsAsync(new[] { userId }, cancellationToken);
        string displayName = displayNames.GetValueOrDefault(userId, "Unknown");

        // Add system message
        Message systemMessage = new()
        {
            ConversationId = request.ConversationId,
            SenderId = userId,
            Content = $"{displayName} joined the group",
            SentAt = now,
            MessageType = MessageType.System,
        };

        _db.Messages.Add(systemMessage);

        // Update conversation's LastMessageAt
        conversation.LastMessageAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
