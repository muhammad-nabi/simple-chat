using SimpleChat.Application.Common.Security;

namespace SimpleChat.Application.Messaging.Commands.CreateConversation;

[Authorize]
public record CreateConversationCommand(
    string? OtherUserId = null,
    List<string>? ParticipantIds = null,
    string? GroupName = null) : IRequest<long>;
