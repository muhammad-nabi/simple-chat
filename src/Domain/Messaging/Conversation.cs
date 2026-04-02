using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Domain.Messaging;

public class Conversation : BaseEntity
{
    public ConversationType Type { get; set; }
    public string? Name { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public DateTimeOffset? LastMessageAt { get; set; }

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
