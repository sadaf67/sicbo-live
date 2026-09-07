using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SicBoLive.Domain.Entities;

namespace SicBoLive.Infrastructure.Persistence.Configurations;

public class BetConfiguration : IEntityTypeConfiguration<Bet>
{
    public void Configure(EntityTypeBuilder<Bet> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Amount).HasPrecision(18, 2);
        builder.Property(b => b.WinAmount).HasPrecision(18, 2);
        builder.HasIndex(b => new { b.GameRoundId, b.PlayerId });
    }
}
