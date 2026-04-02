using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleChat.Domain.Messaging;

namespace SimpleChat.Infrastructure.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.Property(c => c.Type)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasMaxLength(256);

        builder.Property(c => c.CreatedById)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(c => c.CreatedAt)
            .IsRequired();
    }
}
