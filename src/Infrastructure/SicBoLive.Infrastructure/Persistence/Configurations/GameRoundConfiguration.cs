using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SicBoLive.Domain.Entities;

namespace SicBoLive.Infrastructure.Persistence.Configurations;

public class GameRoundConfiguration : IEntityTypeConfiguration<GameRound>
{
    public void Configure(EntityTypeBuilder<GameRound> builder)
    {
        builder.HasKey(r => r.Id);

        builder.OwnsOne(r => r.Result, dice =>
        {
            dice.Property(d => d.Die1).HasColumnName("ResultDie1");
            dice.Property(d => d.Die2).HasColumnName("ResultDie2");
            dice.Property(d => d.Die3).HasColumnName("ResultDie3");
        });

        builder.HasMany(r => r.Bets)
            .WithOne()
            .HasForeignKey(b => b.GameRoundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Bets).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(r => new { r.GroupId, r.RoundNumber });
    }
}
