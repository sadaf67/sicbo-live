using Microsoft.EntityFrameworkCore;
using SicBoLive.Domain.Entities;

namespace SicBoLive.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Group> Groups { get; }
    DbSet<Wallet> Wallets { get; }
    DbSet<TokenTransaction> TokenTransactions { get; }
    DbSet<GameRound> GameRounds { get; }
    DbSet<Bet> Bets { get; }
    DbSet<BetTypeConfig> BetTypeConfigs { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<PlayerSubscription> PlayerSubscriptions { get; }
    DbSet<TokenRequest> TokenRequests { get; }
    DbSet<PendingRegistration> PendingRegistrations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
