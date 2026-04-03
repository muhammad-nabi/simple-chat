using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Domain.Common.Enums;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.Messaging.Queries.GetGroupMembers;

public class GetGroupMembersQueryHandler : IRequestHandler<GetGroupMembersQuery, List<GroupMemberDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IUser _currentUser;
    private readonly IIdentityService _identityService;

    public GetGroupMembersQueryHandler(
        IApplicationDbContext db,
        IUser currentUser,
        IIdentityService identityService)
    {
        _db = db;
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<List<GroupMemberDto>> Handle(GetGroupMembersQuery request, CancellationToken cancellationToken)
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

        bool isParticipant = await _db.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == request.ConversationId && cp.UserId == userId, cancellationToken);

        if (!isParticipant)
        {
            throw new Common.Exceptions.ForbiddenAccessException();
        }

        List<ConversationParticipant> participants = await _db.ConversationParticipants
            .Where(cp => cp.ConversationId == request.ConversationId)
            .OrderBy(cp => cp.JoinedAt)
            .ToListAsync(cancellationToken);

        List<string> participantUserIds = participants.Select(p => p.UserId).ToList();

        Dictionary<string, string> displayNames = await _identityService
            .GetDisplayNamesByIdsAsync(participantUserIds, cancellationToken);

        List<GroupMemberDto> result = participants.Select(p => new GroupMemberDto(
            p.UserId,
            displayNames.GetValueOrDefault(p.UserId, "Unknown"),
            p.JoinedAt)).ToList();

        return result;
    }
}
