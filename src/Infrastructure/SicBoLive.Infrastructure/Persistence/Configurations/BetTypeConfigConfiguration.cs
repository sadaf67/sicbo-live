using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SicBoLive.Domain.Entities;

namespace SicBoLive.Infrastructure.Persistence.Configurations;

public class BetTypeConfigConfiguration : IEntityTypeConfiguration<BetTypeConfig>
{
    public void Configure(EntityTypeBuilder<BetTypeConfig> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Code).HasMaxLength(32).IsRequired();
        builder.HasIndex(b => b.Code).IsUnique();
        builder.Property(b => b.DisplayLabel).HasMaxLength(64).IsRequired();
        builder.Property(b => b.Multiplier).HasPrecision(18, 2);

        builder.Property(b => b.Faces)
            .HasConversion(
                faces => JsonSerializer.Serialize(faces, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<int>>(json, (JsonSerializerOptions?)null) ?? new List<int>(),
                new ValueComparer<IReadOnlyList<int>>(
                    (a, b) => a!.SequenceEqual(b!),
                    f => f.Aggregate(0, (hash, v) => HashCode.Combine(hash, v)),
                    f => f.ToList()))
            .HasMaxLength(32);
    }
}
