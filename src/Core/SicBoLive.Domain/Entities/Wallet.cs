using SicBoLive.Domain.Common;

namespace SicBoLive.Domain.Entities;

/// <summary>A player's virtual token balance inside a single group/table. Never backed by real currency.</summary>
public class Wallet : BaseEntity
{
    public Guid GroupId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Balance { get; private set; }

    private Wallet() { }

    public Wallet(Guid groupId, Guid userId)
    {
        GroupId = groupId;
        UserId = userId;
        Balance = 0;
    }

    public void Credit(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("مبلغ واریزی نمی‌تواند منفی باشد.");
        Balance += amount;
    }

    public void Debit(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("مبلغ برداشت نمی‌تواند منفی باشد.");
        if (amount > Balance) throw new InvalidOperationException("موجودی ژتون کافی نیست.");
        Balance -= amount;
    }

    /// <summary>
    /// Same as <see cref="Debit"/> but without the sufficient-balance guard - the balance is
    /// allowed to go negative. Intended only for a dealer's "house" wallet paying out a player's
    /// winnings: since the tokens are purely virtual (never real currency, never cashed out), a
    /// negative dealer balance is a harmless running IOU, whereas blocking the payout (or letting
    /// a *player* wallet go negative) would not be. Never call this on a player wallet.
    /// </summary>
    public void DebitAllowingNegative(decimal amount)
    {
        if (amount < 0) throw new ArgumentException("مبلغ برداشت نمی‌تواند منفی باشد.");
        Balance -= amount;
    }
}
