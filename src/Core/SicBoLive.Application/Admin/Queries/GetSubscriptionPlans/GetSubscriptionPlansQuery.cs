using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;

namespace SicBoLive.Application.Admin.Queries.GetSubscriptionPlans;

public record SubscriptionPlanDto(
    Guid Id,
    string Name,
    decimal EntryTokenAmount,
    int GameDurationMinutes,
    decimal DiscountPercent,
    decimal EffectiveEntryAmount,
    bool IsActive);

public record GetSubscriptionPlansQuery : IRequest<IReadOnlyList<SubscriptionPlanDto>>;

public class GetSubscriptionPlansQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetSubscriptionPlansQuery, IReadOnlyList<SubscriptionPlanDto>>
{
    public async Task<IReadOnlyList<SubscriptionPlanDto>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await db.SubscriptionPlans.ToListAsync(cancellationToken);

        return plans
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new SubscriptionPlanDto(
                p.Id, p.Name, p.EntryTokenAmount, p.GameDurationMinutes, p.DiscountPercent, p.EffectiveEntryAmount(), p.IsActive))
            .ToList();
    }
}
