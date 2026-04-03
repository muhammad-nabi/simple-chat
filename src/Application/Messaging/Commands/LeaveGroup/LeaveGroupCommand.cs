using SimpleChat.Application.Common.Security;

namespace SimpleChat.Application.Messaging.Commands.LeaveGroup;

[Authorize]
public record LeaveGroupCommand(long ConversationId) : IRequest<Unit>;
