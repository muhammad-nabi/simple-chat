using SimpleChat.Application.Common.Exceptions;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.Messaging.Queries.GetMessageHistory;

public class GetMessageHistoryQueryHandler : IRequestHandler<GetMessageHistoryQuery, MessageHistoryResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IUser _currentUser;
    private readonly IIdentityService _identityService;

    public GetMessageHistoryQueryHandler(
        IApplicationDbContext db,
        IUser currentUser,
        IIdentityService identityService)
    {
        _db = db;
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<MessageHistoryResponse> Handle(GetMessageHistoryQuery request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id
            ?? throw new UnauthorizedAccessException();

        bool isParticipant = await _db.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == request.ConversationId && cp.UserId == userId, cancellationToken);

        if (!isParticipant)
        {
            throw new ForbiddenAccessException();
        }

        IQueryable<Message> query = _db.Messages
            .Where(m => m.ConversationId == request.ConversationId);

        if (request.Before.HasValue)
        {
            query = query.Where(m => m.Id < request.Before.Value);
        }

        List<Message> messages = await query
            .OrderByDescending(m => m.Id)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);

        bool hasMore = messages.Count > request.Limit;
        if (hasMore)
        {
            messages.RemoveAt(messages.Count - 1);
        }

        List<string> senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
        Dictionary<string, string> displayNames = await _identityService
            .GetDisplayNamesByIdsAsync(senderIds, cancellationToken);

        List<MessageDto> messageDtos = messages.Select(m => new MessageDto(
            m.Id,
            m.ConversationId,
            m.SenderId,
            displayNames.GetValueOrDefault(m.SenderId, "Unknown"),
            m.Content,
            m.SentAt,
            m.MessageType.ToString())).ToList();

        long? nextCursor = hasMore && messageDtos.Count > 0
            ? messageDtos[^1].Id
            : null;

        return new MessageHistoryResponse(messageDtos, hasMore, nextCursor);
    }
}
