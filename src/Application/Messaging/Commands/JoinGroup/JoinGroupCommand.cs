using SimpleChat.Application.Common.Security;

namespace SimpleChat.Application.Messaging.Commands.JoinGroup;

[Authorize]
public record JoinGroupCommand(long ConversationId) : IRequest<Unit>;
