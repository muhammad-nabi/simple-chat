using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Infrastructure.Data.Configurations;

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.HasKey(cp => new { cp.ConversationId, cp.UserId });

        builder.Property(cp => cp.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(cp => cp.JoinedAt)
            .IsRequired();

        builder.HasOne(cp => cp.Conversation)
            .WithMany(c => c.Participants)
            .HasForeignKey(cp => cp.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
