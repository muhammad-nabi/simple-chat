using SimpleChat.Domain.Common.Enums;

namespace SimpleChat.Domain.Messaging;

public class Message : BaseEntity
{
    public long ConversationId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
    public MessageType MessageType { get; set; }
    public long? FileId { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
}
