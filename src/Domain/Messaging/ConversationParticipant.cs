namespace SimpleChat.Domain.Messaging;

public class ConversationParticipant
{
    public long ConversationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset JoinedAt { get; set; }
    public long? LastReadMessageId { get; set; }

    public Conversation Conversation { get; set; } = null!;
}
