using SicBoLive.Domain.Common;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Domain.Entities;

public class Bet : BaseEntity
{
    public Guid GameRoundId { get; private set; }
    public Guid PlayerId { get; private set; }
    public Guid BetTypeConfigId { get; private set; }
    public decimal Amount { get; private set; }
    public BetOutcome Outcome { get; private set; } = BetOutcome.Pending;

    /// <summary>Winnings only (stake excluded). Zero when lost.</summary>
    public decimal WinAmount { get; private set; }

    internal Bet(Guid gameRoundId, Guid playerId, Guid betTypeConfigId, decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("مبلغ شرط باید مثبت باشد.");
        GameRoundId = gameRoundId;
        PlayerId = playerId;
        BetTypeConfigId = betTypeConfigId;
        Amount = amount;
    }

    public void Settle(bool won, decimal multiplier)
    {
        if (Outcome != BetOutcome.Pending)
            throw new InvalidOperationException("این شرط قبلاً تسویه شده است.");

        Outcome = won ? BetOutcome.Won : BetOutcome.Lost;
        WinAmount = won ? Amount * multiplier : 0m;
    }

    /// <summary>Relocates this still-pending bet to a different spot without changing its amount - the
    /// grace-period alternative to cancelling once the betting countdown has expired (see
    /// GameRound.EnsureBetMovable). Amount/wallet are untouched; only the target spot changes.</summary>
    public void MoveTo(Guid newBetTypeConfigId)
    {
        if (Outcome != BetOutcome.Pending)
            throw new InvalidOperationException("این شرط قبلاً تسویه شده و دیگر قابل جابه‌جایی نیست.");

        BetTypeConfigId = newBetTypeConfigId;
    }
}
