using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.GameRounds.Queries.GetRoundHistory;

public record RoundHistoryItemDto(Guid RoundId, int RoundNumber, DiceResultDto? Dice, DateTimeOffset? ClosedAt);

/// <summary>Most recent closed rounds for a table, newest first - powers the "دور‌های قبلی" list
/// in the hamburger drawer so players can see what was rolled in previous rounds without having
/// caught the DiceRollOverlay live.</summary>
public record GetRoundHistoryQuery(Guid GroupId, int Take = 20) : IRequest<IReadOnlyList<RoundHistoryItemDto>>;

public class GetRoundHistoryQueryHandler(IApplicationDbContext db) : IRequestHandler<GetRoundHistoryQuery, IReadOnlyList<RoundHistoryItemDto>>
{
    public async Task<IReadOnlyList<RoundHistoryItemDto>> Handle(GetRoundHistoryQuery request, CancellationToken cancellationToken)
    {
        var rounds = await db.GameRounds
            .Where(r => r.GroupId == request.GroupId && r.Status == RoundStatus.Closed)
            .OrderByDescending(r => r.RoundNumber)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return rounds
            .Select(r => new RoundHistoryItemDto(
                r.Id,
                r.RoundNumber,
                r.Result is null ? null : new DiceResultDto(r.Result.Die1, r.Result.Die2, r.Result.Die3, r.Result.Total, r.Result.IsTriple),
                r.ClosedAt))
            .ToList();
    }
}
