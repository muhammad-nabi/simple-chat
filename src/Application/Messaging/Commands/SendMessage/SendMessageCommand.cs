using SimpleChat.Application.Common.Security;

namespace SimpleChat.Application.Messaging.Commands.SendMessage;

[Authorize]
public record SendMessageCommand(long ConversationId, string Content) : IRequest<long>;
