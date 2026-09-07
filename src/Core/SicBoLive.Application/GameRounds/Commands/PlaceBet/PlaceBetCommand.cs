using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.GameRounds.Commands.PlaceBet;

public record PlaceBetCommand(Guid RoundId, string BetTypeCode, decimal Amount) : IRequest<Guid>;

public class PlaceBetCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier)
    : IRequestHandler<PlaceBetCommand, Guid>
{
    // Codes seeded in StandardSicBoBoard.cs for the BIG/SMALL/ODD/EVEN spots - the only bet types
    // Group.RestrictBigSmallOddEvenBets applies its extra rule to: each individual bet must be at
    // least 20x MinBetTokens, the player's pending total on that SAME spot must never exceed
    // group.MaxBetTokens (the dealer's declared table max, checked per spot - e.g. betting on both
    // BIG and ODD is fine as long as neither spot alone passes the cap), and the player can never
    // hold pending bets on both sides of an opposite pair (BIG+SMALL, or ODD+EVEN) at once.
    private static readonly HashSet<string> BigSmallOddEvenCodes = new(StringComparer.OrdinalIgnoreCase) { "BIG", "SMALL", "ODD", "EVEN" };

    private static readonly Dictionary<string, string> OppositeSpotCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BIG"] = "SMALL",
        ["SMALL"] = "BIG",
        ["ODD"] = "EVEN",
        ["EVEN"] = "ODD",
    };

    private static readonly Dictionary<string, string> SpotPersianName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BIG"] = "بیگ",
        ["SMALL"] = "اسمال",
        ["ODD"] = "فرد",
        ["EVEN"] = "زوج",
    };

    public async Task<Guid> Handle(PlaceBetCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != UserRole.Player)
            throw new UnauthorizedAccessException("فقط بازیکنان می‌توانند شرط‌بندی کنند.");

        // Note: EF Core's Sqlite provider can't translate DateTimeOffset comparisons (only equality) to SQL,
        // so RevokedAt == null is filtered server-side and the ExpiresAt check is applied after materializing.
        var now = DateTimeOffset.UtcNow;
        var mySubscriptions = await db.PlayerSubscriptions
            .Where(s => s.UserId == currentUser.UserId && s.RevokedAt == null)
            .ToListAsync(cancellationToken);
        if (!mySubscriptions.Any(s => s.ExpiresAt >= now))
            throw new UnauthorizedAccessException("برای شرط‌بندی باید اشتراک فعال داشته باشید. لطفاً با مدیر سایت تماس بگیرید.");

        var round = await db.GameRounds.FindAsync([request.RoundId], cancellationToken)
            ?? throw new KeyNotFoundException("دور مورد نظر یافت نشد.");

        var group = await db.Groups.FindAsync([round.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        if (request.Amount < group.MinBetTokens || request.Amount > group.MaxBetTokens)
            throw new InvalidOperationException($"مبلغ شرط باید بین {group.MinBetTokens} تا {group.MaxBetTokens} ژتون باشد.");

        var betType = await db.BetTypeConfigs.FirstOrDefaultAsync(
            b => b.Code == request.BetTypeCode && b.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("نوع شرط نامعتبر است.");

        // Cumulative-per-spot ceiling - applies to EVERY bet type, not just BIG/SMALL/ODD/EVEN.
        // The single-bet check above (Amount > group.MaxBetTokens) only ever looked at this ONE
        // incoming bet in isolation, so a player could split one oversized stake into several
        // separate placements on the exact same spot (e.g. three 100-token bets on SMALL in a
        // group with MaxBetTokens=100) and the cumulative total on that spot would silently exceed
        // the dealer's declared max. This closes that gap for the whole 118-spot board: a player's
        // pending total on any ONE spot can never exceed a per-spot ceiling, though they remain free
        // to spread bets across as many different spots as they like.
        //
        // The ceiling itself differs by spot: every non-BIG/SMALL/ODD/EVEN spot on the board -
        // totals, single/double/specific-triple numbers, two/three-number combinations, any-triple -
        // is capped at 20x the table's MinBetTokens ("در تمامی قسمت‌ها بجز اود/ایون/بیگ/اسمال حداکثر
        // مجموع ژتون که میشد در یک خانه گذاشت ۲۰ برابر حداقل انتخاب شده میتواند باشد"), while
        // BIG/SMALL/ODD/EVEN themselves are capped at 5x the table's MaxBetTokens ("حداکثر مبلغ
        // شرط‌بندی در این خانه‌ها باید ۵ برابر حداکثر مبلغی که دیلر در افتتاح میز تعیین کرده باشد") -
        // corrected from an earlier mistaken 5x MinBetTokens reading, which produced a ceiling far
        // below the OPTIONAL group.RestrictBigSmallOddEvenBets floor (20x MinBetTokens) and made
        // those four spots unbettable whenever a dealer turned that toggle on. Scaling off
        // MaxBetTokens instead keeps the two independent, since MaxBetTokens is set well above
        // MinBetTokens on any real table, so 5x MaxBetTokens comfortably exceeds the 20x MinBetTokens
        // floor and both rules can be satisfied together.
        var spotCeiling = BigSmallOddEvenCodes.Contains(request.BetTypeCode) ? group.MaxBetTokens * 5 : group.MinBetTokens * 20;
        var existingOnThisSpot = await db.Bets
            .Where(b => b.GameRoundId == round.Id
                && b.PlayerId == currentUser.UserId
                && b.Outcome == BetOutcome.Pending
                && b.BetTypeConfigId == betType.Id)
            .SumAsync(b => b.Amount, cancellationToken);
        if (existingOnThisSpot + request.Amount > spotCeiling)
            throw new InvalidOperationException($"مجموع شرط‌های شما روی این خانه نمی‌تواند بیشتر از {spotCeiling} ژتون شود.");

        if (group.RestrictBigSmallOddEvenBets && BigSmallOddEvenCodes.Contains(request.BetTypeCode))
        {
            var min = group.MinBetTokens * 20;
            if (request.Amount < min)
                throw new InvalidOperationException($"در این میز، مبلغ شرط روی خانه‌های بیگ/اسمال/فرد/زوج باید حداقل {min} ژتون باشد.");

            // BIG/SMALL and ODD/EVEN are opposite pairs - never let the player hold pending bets on
            // both sides of the same pair at once.
            var oppositeCode = OppositeSpotCode[request.BetTypeCode];
            var oppositeBetTypeId = await db.BetTypeConfigs
                .Where(b => b.Code == oppositeCode)
                .Select(b => b.Id)
                .FirstAsync(cancellationToken);
            var hasOppositeBet = await db.Bets.AnyAsync(b => b.GameRoundId == round.Id
                && b.PlayerId == currentUser.UserId
                && b.Outcome == BetOutcome.Pending
                && b.BetTypeConfigId == oppositeBetTypeId, cancellationToken);
            if (hasOppositeBet)
                throw new InvalidOperationException($"در این میز، نمی‌توانید همزمان روی خانه‌های {SpotPersianName[request.BetTypeCode]} و {SpotPersianName[oppositeCode]} شرط ببندید.");
        }

        var wallet = await db.Wallets.FirstOrDefaultAsync(
            w => w.GroupId == round.GroupId && w.UserId == currentUser.UserId, cancellationToken)
            ?? throw new InvalidOperationException("قبل از شرط‌بندی باید به میز بپیوندید — هنوز کیف پولی در این میز ندارید.");

        wallet.Debit(request.Amount);
        var bet = round.PlaceBet(currentUser.UserId, betType.Id, request.Amount);
        db.Bets.Add(bet);
        db.TokenTransactions.Add(new TokenTransaction(round.GroupId, wallet.Id, request.Amount, TokenTransactionType.BetPlaced, currentUser.UserId, betType.Code));

        await db.SaveChangesAsync(cancellationToken);
        await notifier.WalletUpdated(new WalletBalanceDto(round.GroupId, currentUser.UserId, wallet.Balance), cancellationToken);

        return bet.Id;
    }
}
