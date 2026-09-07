using SicBoLive.Domain.Common;
using SicBoLive.Domain.Enums;
using SicBoLive.Domain.ValueObjects;

namespace SicBoLive.Domain.Entities;

public class GameRound : BaseEntity
{
    public Guid GroupId { get; private set; }
    public Guid DealerId { get; private set; }
    public int RoundNumber { get; private set; }
    public RoundStatus Status { get; private set; }
    public DateTimeOffset BettingEndsAt { get; private set; }
    public DiceRoll? Result { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private readonly List<Bet> _bets = new();
    public IReadOnlyCollection<Bet> Bets => _bets.AsReadOnly();

    private GameRound() { }

    public GameRound(Guid groupId, Guid dealerId, int roundNumber, int bettingWindowSeconds)
    {
        GroupId = groupId;
        DealerId = dealerId;
        RoundNumber = roundNumber;
        Status = RoundStatus.Betting;
        BettingEndsAt = DateTimeOffset.UtcNow.AddSeconds(bettingWindowSeconds);
    }

    public Bet PlaceBet(Guid playerId, Guid betTypeConfigId, decimal amount)
    {
        if (Status != RoundStatus.Betting)
            throw new InvalidOperationException("شرط‌بندی برای این دور بسته شده است.");
        if (DateTimeOffset.UtcNow > BettingEndsAt)
            throw new InvalidOperationException("مهلت شرط‌بندی به پایان رسیده است.");

        var bet = new Bet(Id, playerId, betTypeConfigId, amount);
        _bets.Add(bet);
        return bet;
    }

    /// <summary>
    /// Guard for a player taking back a bet before the betting countdown expires (changed their mind).
    /// Once BettingEndsAt has passed, Status is still Betting until the dealer actually rolls (see
    /// RollDiceCommand, which calls CloseBetting/RollDice/Close synchronously in one request) - that
    /// gap is the "grace period" where cancelling is no longer allowed, but moving to another spot still
    /// is (see EnsureBetMovable). The actual Bet row removal happens at the persistence layer (see
    /// CancelBetCommand) rather than through the in-memory <see cref="_bets"/> collection, since the
    /// round is looked up standalone (without Include(r => r.Bets)) for that command.
    /// </summary>
    public void EnsureBetCancellable()
    {
        if (Status != RoundStatus.Betting)
            throw new InvalidOperationException("دیگر امکان پس گرفتن شرط وجود ندارد — شرط‌بندی این دور بسته شده است.");
        if (DateTimeOffset.UtcNow > BettingEndsAt)
            throw new InvalidOperationException("مهلت شرط‌بندی به پایان رسیده است — دیگر نمی‌توانید شرط را پس بگیرید، اما می‌توانید آن را به خانه دیگری منتقل کنید.");
    }

    /// <summary>
    /// Guard for a player relocating a still-pending bet to a different spot (see Bet.MoveTo). Unlike
    /// EnsureBetCancellable, this deliberately has no BettingEndsAt check: moving stays allowed through
    /// the grace period right up until the dealer closes betting to roll the dice (Status leaves Betting).
    /// </summary>
    public void EnsureBetMovable()
    {
        if (Status != RoundStatus.Betting)
            throw new InvalidOperationException("دیگر امکان جابه‌جایی شرط وجود ندارد — شرط‌بندی این دور بسته شده است.");
    }

    public void CloseBetting()
    {
        if (Status != RoundStatus.Betting)
            throw new InvalidOperationException("این دور در مرحله شرط‌بندی نیست.");
        Status = RoundStatus.Rolling;
    }

    public DiceRoll RollDice(Random? rng = null)
    {
        if (Status != RoundStatus.Rolling)
            throw new InvalidOperationException("دیلر باید قبل از انداختن تاس، شرط‌بندی را ببندد.");
        Result = DiceRoll.Random(rng);
        Status = RoundStatus.Result;
        return Result;
    }

    public void Close()
    {
        if (Status != RoundStatus.Result)
            throw new InvalidOperationException("دور باید قبل از بسته شدن، نتیجه داشته باشد.");
        Status = RoundStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
    }
}
