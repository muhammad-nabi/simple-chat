using SimpleChat.Application.Common.Exceptions;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.Notifications;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.Messaging.Commands.SendMessage;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, long>
{
    private readonly IApplicationDbContext _db;
    private readonly IUser _currentUser;
    private readonly IPublisher _publisher;

    public SendMessageCommandHandler(
        IApplicationDbContext db,
        IUser currentUser,
        IPublisher publisher)
    {
        _db = db;
        _currentUser = currentUser;
        _publisher = publisher;
    }

    public async Task<long> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id
            ?? throw new UnauthorizedAccessException();

        bool isParticipant = await _db.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == request.ConversationId && cp.UserId == userId, cancellationToken);

        if (!isParticipant)
        {
            throw new ForbiddenAccessException();
        }

        Conversation conversation = await _db.Conversations
            .FirstAsync(c => c.Id == request.ConversationId, cancellationToken);

        Message message = new()
        {
            ConversationId = request.ConversationId,
            SenderId = userId,
            Content = request.Content,
            SentAt = DateTimeOffset.UtcNow,
            MessageType = MessageType.Text,
        };

        _db.Messages.Add(message);
        conversation.LastMessageAt = message.SentAt;
        await _db.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(new MessageSent(message, message.ConversationId), cancellationToken);

        return message.Id;
    }
}
