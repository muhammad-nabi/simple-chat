using SimpleChat.Application.Common.Security;

namespace SimpleChat.Application.Messaging.Queries.GetConversations;

[Authorize]
public record GetConversationsQuery : IRequest<List<ConversationListDto>>;
