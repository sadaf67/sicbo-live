using SicBoLive.Application.Common.Models;

namespace SicBoLive.Application.Common.Interfaces;

/// <summary>Pushes real-time game events to everyone connected to a group's table (implemented over SignalR).</summary>
public interface IGameNotifier
{
    Task RoundStarted(RoundStartedDto round, CancellationToken cancellationToken = default);
    Task BettingClosed(Guid groupId, Guid roundId, CancellationToken cancellationToken = default);
    Task DiceRolled(Guid groupId, Guid roundId, DiceResultDto dice, CancellationToken cancellationToken = default);
    Task RoundSettled(RoundResultDto result, CancellationToken cancellationToken = default);
    Task WalletUpdated(WalletBalanceDto wallet, CancellationToken cancellationToken = default);
    Task ChatMessagePosted(Guid groupId, Guid senderId, string senderName, string text, DateTimeOffset sentAt, CancellationToken cancellationToken = default);
    /// <summary>Delivered only to the sender's and recipient's own connections (via SignalR user targeting), never broadcast to the whole table.</summary>
    Task PrivateChatMessagePosted(Guid groupId, Guid senderId, string senderName, Guid recipientId, string text, DateTimeOffset sentAt, CancellationToken cancellationToken = default);
    Task TokenRequested(TokenRequestDto request, CancellationToken cancellationToken = default);
    Task TokenRequestResolved(TokenRequestDto request, CancellationToken cancellationToken = default);
    /// <summary>Broadcast to the whole table (dealer + players) whenever the dealer changes the min/max bet limits, so everyone's UI updates live instead of only on next refresh.</summary>
    Task TokenLimitsUpdated(TokenLimitsUpdatedDto limits, CancellationToken cancellationToken = default);
    /// <summary>Broadcast to the whole table whenever the dealer changes the per-round betting countdown duration (only affects rounds started after the change).</summary>
    Task BettingWindowUpdated(BettingWindowUpdatedDto window, CancellationToken cancellationToken = default);
}
