using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.Messaging.Queries.GetConversations;

public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, List<ConversationListDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IUser _currentUser;
    private readonly IIdentityService _identityService;

    public GetConversationsQueryHandler(
        IApplicationDbContext db,
        IUser currentUser,
        IIdentityService identityService)
    {
        _db = db;
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<List<ConversationListDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id
            ?? throw new UnauthorizedAccessException();

        // Get all conversation IDs the user participates in
        List<ConversationParticipant> userParticipations = await _db.ConversationParticipants
            .Where(cp => cp.UserId == userId)
            .ToListAsync(cancellationToken);

        if (userParticipations.Count == 0)
        {
            return new List<ConversationListDto>();
        }

        List<long> conversationIds = userParticipations.Select(cp => cp.ConversationId).ToList();

        // Load conversations
        List<Conversation> conversations = await _db.Conversations
            .Where(c => conversationIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        // Get the latest message per conversation for preview (correlated subquery — avoids GroupBy translation issues)
        List<long> latestMessageIds = await _db.Messages
            .Where(m => conversationIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.Max(m => m.Id))
            .ToListAsync(cancellationToken);

        List<Message> latestMessages = await _db.Messages
            .Where(m => latestMessageIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        Dictionary<long, Message> latestMessageByConversation = latestMessages
            .ToDictionary(m => m.ConversationId);

        // Get all participants for these conversations (for display names)
        List<ConversationParticipant> allParticipants = await _db.ConversationParticipants
            .Where(cp => conversationIds.Contains(cp.ConversationId))
            .ToListAsync(cancellationToken);

        // Get display names for all participant user IDs
        List<string> allUserIds = allParticipants.Select(cp => cp.UserId).Distinct().ToList();
        Dictionary<string, string> displayNames = await _identityService
            .GetDisplayNamesByIdsAsync(allUserIds, cancellationToken);

        // Compute unread counts per conversation (batched query — excludes user's own messages)
        Dictionary<long, int> unreadCounts = new();
        foreach (ConversationParticipant participation in userParticipations)
        {
            long? lastReadMessageId = participation.LastReadMessageId;
            int unreadCount = await _db.Messages
                .Where(m => m.ConversationId == participation.ConversationId)
                .Where(m => m.SenderId != userId)
                .Where(m => lastReadMessageId == null || m.Id > lastReadMessageId.Value)
                .CountAsync(cancellationToken);

            unreadCounts[participation.ConversationId] = unreadCount;
        }

        // Build DTOs
        List<ConversationListDto> result = conversations.Select(c =>
        {
            List<ParticipantDto> otherParticipants = allParticipants
                .Where(cp => cp.ConversationId == c.Id && cp.UserId != userId)
                .Select(cp => new ParticipantDto(
                    cp.UserId,
                    displayNames.GetValueOrDefault(cp.UserId, "Unknown")))
                .ToList();

            string? lastMessagePreview = latestMessageByConversation.TryGetValue(c.Id, out Message? latest)
                ? TruncatePreview(latest.Content)
                : null;

            return new ConversationListDto(
                c.Id,
                c.Type.ToString(),
                c.Name,
                lastMessagePreview,
                c.LastMessageAt,
                otherParticipants,
                unreadCounts.GetValueOrDefault(c.Id, 0));
        })
        .OrderByDescending(c => c.LastMessageAt)
        .ToList();

        return result;
    }

    private static string? TruncatePreview(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        return content.Length <= 100 ? content : string.Concat(content.AsSpan(0, 97), "...");
    }
}
