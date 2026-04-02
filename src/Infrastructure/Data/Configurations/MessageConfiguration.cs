using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Infrastructure.Data.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.Property(m => m.ConversationId)
            .IsRequired();

        builder.Property(m => m.SenderId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(m => m.SentAt)
            .IsRequired();

        builder.Property(m => m.MessageType)
            .IsRequired();

        builder.HasIndex(m => new { m.ConversationId, m.Id })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Messages_ConversationId_Id");

        builder.HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
