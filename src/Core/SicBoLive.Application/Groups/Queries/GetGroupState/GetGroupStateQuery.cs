using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Groups.Queries.GetGroupState;

public record ActiveRoundDto(Guid RoundId, int RoundNumber, string Status, DateTimeOffset BettingEndsAt, int? Die1, int? Die2, int? Die3);

public record MyBetDto(Guid BetId, string BetTypeCode, decimal Amount, string Outcome, decimal WinAmount);

/// <summary>Every player's bets in the active round (dealer + players alike can see who bet what,
/// via the "شرط‌های همه بازیکنان" drawer panel) - unlike MyBetDto, this carries the bettor's identity.</summary>
public record AllBetDto(Guid BetId, Guid PlayerId, string PlayerDisplayName, string BetTypeCode, decimal Amount, string Outcome, decimal WinAmount);

public record GroupStateDto(
    Guid GroupId,
    string Name,
    string InviteCode,
    decimal MinBetTokens,
    decimal MaxBetTokens,
    int BettingWindowSeconds,
    bool IsActive,
    decimal MyBalance,
    ActiveRoundDto? ActiveRound,
    IReadOnlyList<MyBetDto> MyBetsInActiveRound,
    IReadOnlyList<AllBetDto> AllBetsInActiveRound,
    bool HasActiveSubscription,
    TokenRequestDto? MyPendingTokenRequest);

public record GetGroupStateQuery(Guid GroupId) : IRequest<GroupStateDto>;

public class GetGroupStateQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser, IUserDirectory userDirectory)
    : IRequestHandler<GetGroupStateQuery, GroupStateDto>
{
    public async Task<GroupStateDto> Handle(GetGroupStateQuery request, CancellationToken cancellationToken)
    {
        var group = await db.Groups.FindAsync([request.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        var wallet = await db.Wallets.FirstOrDefaultAsync(
            w => w.GroupId == request.GroupId && w.UserId == currentUser.UserId, cancellationToken);

        var round = await db.GameRounds
            .Where(r => r.GroupId == request.GroupId && r.Status != RoundStatus.Closed)
            .OrderByDescending(r => r.RoundNumber)
            .FirstOrDefaultAsync(cancellationToken);

        ActiveRoundDto? activeRoundDto = null;
        var myBets = new List<MyBetDto>();
        var allBets = new List<AllBetDto>();

        if (round is not null)
        {
            activeRoundDto = new ActiveRoundDto(
                round.Id, round.RoundNumber, round.Status.ToString(), round.BettingEndsAt,
                round.Result?.Die1, round.Result?.Die2, round.Result?.Die3);

            var betTypeCodesById = await db.BetTypeConfigs.ToDictionaryAsync(b => b.Id, b => b.Code, cancellationToken);

            // Fetched once and split in-memory into "mine" vs "everyone's" so both lists share a
            // single round-trip instead of querying db.Bets twice for the same round.
            var roundBets = await db.Bets
                .Where(b => b.GameRoundId == round.Id)
                .ToListAsync(cancellationToken);

            myBets = roundBets
                .Where(b => b.PlayerId == currentUser.UserId)
                .Select(b => new MyBetDto(b.Id, betTypeCodesById[b.BetTypeConfigId], b.Amount, b.Outcome.ToString(), b.WinAmount))
                .ToList();

            var namesByPlayerId = await userDirectory.GetDisplayNamesAsync(
                roundBets.Select(b => b.PlayerId).Distinct(), cancellationToken);

            allBets = roundBets
                .Select(b => new AllBetDto(
                    b.Id, b.PlayerId, namesByPlayerId.GetValueOrDefault(b.PlayerId, "بازیکن"),
                    betTypeCodesById[b.BetTypeConfigId], b.Amount, b.Outcome.ToString(), b.WinAmount))
                .ToList();
        }

        // Betting access is only ever gated for players — dealers/admins aren't subject to the subscription gate.
        // Note: EF Core's Sqlite provider can't translate DateTimeOffset comparisons (only equality) to SQL,
        // so RevokedAt == null is filtered server-side and the ExpiresAt check is applied after materializing.
        var now = DateTimeOffset.UtcNow;
        var hasActiveSubscription = currentUser.Role != UserRole.Player;
        if (!hasActiveSubscription)
        {
            var subscriptions = await db.PlayerSubscriptions
                .Where(s => s.UserId == currentUser.UserId && s.RevokedAt == null)
                .ToListAsync(cancellationToken);
            hasActiveSubscription = subscriptions.Any(s => s.ExpiresAt >= now);
        }

        var myPendingRequest = await db.TokenRequests
            .Where(r => r.GroupId == request.GroupId && r.PlayerId == currentUser.UserId && r.Status == TokenRequestStatus.Pending)
            .Select(r => new TokenRequestDto(r.Id, r.GroupId, r.PlayerId, string.Empty, r.Amount, r.Status.ToString(), r.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return new GroupStateDto(
            group.Id, group.Name, group.InviteCode, group.MinBetTokens, group.MaxBetTokens, group.BettingWindowSeconds, group.IsActive,
            wallet?.Balance ?? 0m, activeRoundDto, myBets, allBets, hasActiveSubscription, myPendingRequest);
    }
}
