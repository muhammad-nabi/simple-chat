using SimpleChat.Application.Common.Security;

namespace SimpleChat.Application.Messaging.Commands.InviteToGroup;

[Authorize]
public record InviteToGroupCommand(long ConversationId, List<string> UserIds) : IRequest<Unit>;
