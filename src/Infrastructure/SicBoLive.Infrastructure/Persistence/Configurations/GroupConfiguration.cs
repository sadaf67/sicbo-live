using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SicBoLive.Domain.Entities;

namespace SicBoLive.Infrastructure.Persistence.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.Property(g => g.InviteCode).HasMaxLength(16).IsRequired();
        builder.HasIndex(g => g.InviteCode).IsUnique();
        builder.Property(g => g.MinBetTokens).HasPrecision(18, 2);
        builder.Property(g => g.MaxBetTokens).HasPrecision(18, 2);
    }
}
