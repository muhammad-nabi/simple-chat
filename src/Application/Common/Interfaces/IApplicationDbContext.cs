using SimpleChat.Domain.Messaging;

namespace SimpleChat.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Conversation> Conversations { get; }
    DbSet<ConversationParticipant> ConversationParticipants { get; }
    DbSet<Message> Messages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
