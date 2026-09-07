using SicBoLive.Domain.Common;

namespace SicBoLive.Domain.Entities;

/// <summary>A dealer's own table/room. Players join a group to play against that dealer's shoe.</summary>
public class Group : BaseEntity
{
    public string Name { get; private set; } = default!;
    public Guid DealerId { get; private set; }
    public string InviteCode { get; private set; } = default!;
    public decimal MinBetTokens { get; private set; }
    public decimal MaxBetTokens { get; private set; }
    public int BettingWindowSeconds { get; private set; } = 30;
    public bool IsActive { get; private set; } = true;
    /// <summary>Dealer-opt-in guard, set once at table creation: when true, PlaceBetCommand additionally
    /// requires bets on the BIG/SMALL/ODD/EVEN spots to fall within [20x, 100x] of MinBetTokens, on top
    /// of the normal MinBetTokens/MaxBetTokens range that applies to every other bet type.</summary>
    public bool RestrictBigSmallOddEvenBets { get; private set; }

    private Group() { }

    public Group(string name, Guid dealerId, decimal minBetTokens, decimal maxBetTokens, int bettingWindowSeconds = 30, bool restrictBigSmallOddEvenBets = false)
    {
        if (minBetTokens <= 0 || maxBetTokens < minBetTokens)
            throw new ArgumentException("محدودیت‌های ژتون نامعتبر است.");

        Name = name;
        DealerId = dealerId;
        InviteCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        MinBetTokens = minBetTokens;
        MaxBetTokens = maxBetTokens;
        BettingWindowSeconds = bettingWindowSeconds;
        RestrictBigSmallOddEvenBets = restrictBigSmallOddEvenBets;
    }

    public void SetTokenLimits(decimal min, decimal max)
    {
        if (min <= 0 || max < min)
            throw new ArgumentException("محدودیت‌های ژتون نامعتبر است.");
        MinBetTokens = min;
        MaxBetTokens = max;
    }

    public void SetBettingWindow(int seconds)
    {
        if (seconds <= 0) throw new ArgumentException("زمان شرط‌بندی باید مثبت باشد.");
        BettingWindowSeconds = seconds;
    }

    public void Close() => IsActive = false;
    public void Reopen() => IsActive = true;
}
