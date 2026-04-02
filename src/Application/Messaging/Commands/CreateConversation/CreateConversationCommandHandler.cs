using SimpleChat.Application.Common.Exceptions;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.Messaging.Commands.CreateConversation;

public class CreateConversationCommandHandler : IRequestHandler<CreateConversationCommand, long>
{
    private readonly IApplicationDbContext _db;
    private readonly IUser _currentUser;
    private readonly IIdentityService _identityService;

    public CreateConversationCommandHandler(
        IApplicationDbContext db,
        IUser currentUser,
        IIdentityService identityService)
    {
        _db = db;
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<long> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        string currentUserId = _currentUser.Id
            ?? throw new UnauthorizedAccessException();

        if (request.OtherUserId == currentUserId)
        {
            throw new Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure("OtherUserId", "Cannot create a conversation with yourself.") });
        }

        bool otherUserExists = await _identityService.UserExistsAsync(request.OtherUserId, cancellationToken);
        if (!otherUserExists)
        {
            throw new Common.Exceptions.NotFoundException("User", request.OtherUserId);
        }

        // Check if a private conversation already exists between these two users
        long? existingId = await _db.Conversations
            .Where(c => c.Type == ConversationType.Private)
            .Where(c => c.Participants.Any(p => p.UserId == currentUserId))
            .Where(c => c.Participants.Any(p => p.UserId == request.OtherUserId))
            .Select(c => (long?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId.HasValue)
        {
            return existingId.Value;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        Conversation conversation = new()
        {
            Type = ConversationType.Private,
            Name = null,
            CreatedById = currentUserId,
        };

        conversation.Participants.Add(new ConversationParticipant
        {
            UserId = currentUserId,
            JoinedAt = now,
        });

        conversation.Participants.Add(new ConversationParticipant
        {
            UserId = request.OtherUserId,
            JoinedAt = now,
        });

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync(cancellationToken);

        return conversation.Id;
    }
}
