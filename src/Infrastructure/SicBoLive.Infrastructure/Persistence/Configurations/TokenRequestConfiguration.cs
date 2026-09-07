using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SicBoLive.Domain.Entities;

namespace SicBoLive.Infrastructure.Persistence.Configurations;

public class TokenRequestConfiguration : IEntityTypeConfiguration<TokenRequest>
{
    public void Configure(EntityTypeBuilder<TokenRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Amount).HasPrecision(18, 2);
        builder.HasIndex(r => new { r.GroupId, r.Status });
    }
}
