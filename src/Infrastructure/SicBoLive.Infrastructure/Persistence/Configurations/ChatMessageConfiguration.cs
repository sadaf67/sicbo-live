using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SicBoLive.Domain.Entities;

namespace SicBoLive.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Text).HasMaxLength(500).IsRequired();
        builder.HasIndex(c => new { c.GroupId, c.CreatedAt });
        // Speeds up loading one private conversation's history (SenderId/RecipientId pair within a
        // group, see GetPrivateChatHistoryQuery) - RecipientId is null for public chat rows, so this
        // index is naturally skipped/sparse for the (far more numerous) public messages.
        builder.HasIndex(c => new { c.GroupId, c.RecipientId, c.SenderId, c.CreatedAt });
    }
}
