using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.GameRounds.Commands.MoveBet;

/// <summary>Player relocates a still-pending bet to a different spot without changing its amount -
/// the grace-period alternative to CancelBetCommand once the betting countdown has expired but the
/// dealer hasn't closed betting to roll yet (see GameRound.EnsureBetMovable). Amount/wallet are
/// untouched; only the target spot changes, so unlike CancelBetCommand this never touches Wallet or
/// TokenTransaction - the stake was already debited when the bet was first placed and stays debited.</summary>
public record MoveBetCommand(Guid BetId, string NewBetTypeCode) : IRequest<Unit>;

public class MoveBetCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier)
    : IRequestHandler<MoveBetCommand, Unit>
{
    // Same restricted-range rule PlaceBetCommand enforces at placement time - re-checked here so a
    // move can't be used to sidestep it by relocating an oversized/undersized bet onto one of these
    // spots after the fact.
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

    public async Task<Unit> Handle(MoveBetCommand request, CancellationToken cancellationToken)
    {
        var bet = await db.Bets.FirstOrDefaultAsync(b => b.Id == request.BetId, cancellationToken)
            ?? throw new KeyNotFoundException("شرط مورد نظر یافت نشد.");

        if (bet.PlayerId != currentUser.UserId)
            throw new UnauthorizedAccessException("شما نمی‌توانید شرط بازیکن دیگری را جابه‌جا کنید.");

        if (bet.Outcome != BetOutcome.Pending)
            throw new InvalidOperationException("این شرط قبلاً تسویه شده و دیگر قابل جابه‌جایی نیست.");

        var round = await db.GameRounds.FindAsync([bet.GameRoundId], cancellationToken)
            ?? throw new KeyNotFoundException("دور مورد نظر یافت نشد.");

        round.EnsureBetMovable();

        var group = await db.Groups.FindAsync([round.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        var newBetType = await db.BetTypeConfigs.FirstOrDefaultAsync(
            b => b.Code == request.NewBetTypeCode && b.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("نوع شرط مقصد نامعتبر است.");

        // Cumulative-per-spot ceiling - applies to EVERY bet type, mirroring the same check
        // PlaceBetCommand now enforces at placement time (see the comment there for why and for the
        // per-spot ceiling's two tiers: 5x MaxBetTokens for BIG/SMALL/ODD/EVEN, 20x MinBetTokens
        // for every other spot). Only the destination spot's OWN pending total matters. The bet being
        // moved keeps its own amount, so the destination spot's other pending bets (excluding this
        // one, since it isn't there yet) plus this bet's amount must still fit under that ceiling -
        // otherwise a move could be used to pile multiple bets onto one already-full spot.
        var spotCeiling = BigSmallOddEvenCodes.Contains(newBetType.Code) ? group.MaxBetTokens * 5 : group.MinBetTokens * 20;
        var existingOnDestinationSpot = await db.Bets
            .Where(b => b.GameRoundId == round.Id
                && b.PlayerId == currentUser.UserId
                && b.Outcome == BetOutcome.Pending
                && b.Id != bet.Id
                && b.BetTypeConfigId == newBetType.Id)
            .SumAsync(b => b.Amount, cancellationToken);
        if (existingOnDestinationSpot + bet.Amount > spotCeiling)
            throw new InvalidOperationException($"مجموع شرط‌های شما روی این خانه نمی‌تواند بیشتر از {spotCeiling} ژتون شود — این شرط را نمی‌توان به این خانه منتقل کرد.");

        if (group.RestrictBigSmallOddEvenBets && BigSmallOddEvenCodes.Contains(newBetType.Code))
        {
            var min = group.MinBetTokens * 20;
            if (bet.Amount < min)
                throw new InvalidOperationException($"در این میز، مبلغ شرط روی خانه‌های بیگ/اسمال/فرد/زوج باید حداقل {min} ژتون باشد — این شرط را نمی‌توان به این خانه منتقل کرد.");

            // BIG/SMALL and ODD/EVEN are opposite pairs - never let the player hold pending bets on
            // both sides at once. Excludes the bet being moved itself: if it's currently sitting on
            // the opposite spot, moving it away from there is exactly what resolves the conflict.
            var oppositeCode = OppositeSpotCode[newBetType.Code];
            var oppositeBetTypeId = await db.BetTypeConfigs
                .Where(b => b.Code == oppositeCode)
                .Select(b => b.Id)
                .FirstAsync(cancellationToken);
            var hasOppositeBet = await db.Bets.AnyAsync(b => b.GameRoundId == round.Id
                && b.PlayerId == currentUser.UserId
                && b.Outcome == BetOutcome.Pending
                && b.Id != bet.Id
                && b.BetTypeConfigId == oppositeBetTypeId, cancellationToken);
            if (hasOppositeBet)
                throw new InvalidOperationException($"در این میز، نمی‌توانید همزمان روی خانه‌های {SpotPersianName[newBetType.Code]} و {SpotPersianName[oppositeCode]} شرط ببندید — این شرط را نمی‌توان به این خانه منتقل کرد.");
        }

        bet.MoveTo(newBetType.Id);
        await db.SaveChangesAsync(cancellationToken);

        // No wallet change to report - piggyback on WalletUpdated purely to trigger every connected
        // client's refreshState() so the board reflects the chip's new spot live (same pattern used
        // elsewhere for "needs a live refresh but no new event payload" cases).
        var wallet = await db.Wallets.FirstOrDefaultAsync(
            w => w.GroupId == round.GroupId && w.UserId == currentUser.UserId, cancellationToken);
        if (wallet is not null)
            await notifier.WalletUpdated(new WalletBalanceDto(round.GroupId, currentUser.UserId, wallet.Balance), cancellationToken);

        return Unit.Value;
    }
}
