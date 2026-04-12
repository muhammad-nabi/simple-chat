using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.Messaging.Queries.GetBrowseGroups;

public class GetBrowseGroupsQueryHandler : IRequestHandler<GetBrowseGroupsQuery, List<BrowseGroupDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IUser _currentUser;

    public GetBrowseGroupsQueryHandler(
        IApplicationDbContext db,
        IUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<BrowseGroupDto>> Handle(GetBrowseGroupsQuery request, CancellationToken cancellationToken)
    {
        string userId = _currentUser.Id
            ?? throw new UnauthorizedAccessException();

        // Get all conversation IDs the user already participates in
        List<long> userConversationIds = await _db.ConversationParticipants
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.ConversationId)
            .ToListAsync(cancellationToken);

        // Get all Group conversations the user is NOT in
        List<Conversation> availableGroups = await _db.Conversations
            .Where(c => c.Type == ConversationType.Group)
            .Where(c => !userConversationIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (availableGroups.Count == 0)
        {
            return new List<BrowseGroupDto>();
        }

        List<long> groupIds = availableGroups.Select(c => c.Id).ToList();

        // Count participants per group
        List<ParticipantCountResult> participantCounts = await _db.ConversationParticipants
            .Where(cp => groupIds.Contains(cp.ConversationId))
            .GroupBy(cp => cp.ConversationId)
            .Select(g => new ParticipantCountResult(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        Dictionary<long, int> countByConversation = participantCounts
            .ToDictionary(r => r.ConversationId, r => r.Count);

        // Get latest message per group for preview
        List<long> latestMessageIds = await _db.Messages
            .Where(m => groupIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.Max(m => m.Id))
            .ToListAsync(cancellationToken);

        List<Message> latestMessages = await _db.Messages
            .Where(m => latestMessageIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        Dictionary<long, Message> latestMessageByGroup = latestMessages
            .ToDictionary(m => m.ConversationId);

        // Build DTOs
        List<BrowseGroupDto> result = availableGroups.Select(c =>
        {
            string? lastMessagePreview = latestMessageByGroup.TryGetValue(c.Id, out Message? latest)
                ? TruncatePreview(latest.Content)
                : null;

            return new BrowseGroupDto(
                c.Id,
                c.Name ?? "Unnamed Group",
                countByConversation.GetValueOrDefault(c.Id, 0),
                lastMessagePreview,
                c.LastMessageAt);
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

    private record ParticipantCountResult(long ConversationId, int Count);
}
