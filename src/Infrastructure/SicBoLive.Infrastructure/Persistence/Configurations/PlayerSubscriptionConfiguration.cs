using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SicBoLive.Domain.Entities;

namespace SicBoLive.Infrastructure.Persistence.Configurations;

public class PlayerSubscriptionConfiguration : IEntityTypeConfiguration<PlayerSubscription>
{
    public void Configure(EntityTypeBuilder<PlayerSubscription> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.UserId);
    }
}
