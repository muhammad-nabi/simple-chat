using SimpleChat.Application.Common.Security;

namespace SimpleChat.Application.Messaging.Queries.GetMessageHistory;

[Authorize]
public record GetMessageHistoryQuery(long ConversationId, long? Before, int Limit = 50) : IRequest<MessageHistoryResponse>;
