using SimpleChat.Application.Common.Security;

namespace SimpleChat.Application.Messaging.Queries.GetGroupMembers;

[Authorize]
public record GetGroupMembersQuery(long ConversationId) : IRequest<List<GroupMemberDto>>;
