using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Entities;
using SicBoLive.Infrastructure.Identity;

namespace SicBoLive.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IApplicationDbContext
{
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<TokenTransaction> TokenTransactions => Set<TokenTransaction>();
    public DbSet<GameRound> GameRounds => Set<GameRound>();
    public DbSet<Bet> Bets => Set<Bet>();
    public DbSet<BetTypeConfig> BetTypeConfigs => Set<BetTypeConfig>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<PlayerSubscription> PlayerSubscriptions => Set<PlayerSubscription>();
    public DbSet<TokenRequest> TokenRequests => Set<TokenRequest>();
    public DbSet<PendingRegistration> PendingRegistrations => Set<PendingRegistration>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
