using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;

namespace SicBoLive.Application.BetTypes.Queries.GetBetTypes;

public record BetTypeDto(Guid Id, string Code, string DisplayLabel, string Category, decimal Multiplier, IReadOnlyList<int> Faces, int? RequiredTotal);

public record GetBetTypesQuery : IRequest<IReadOnlyList<BetTypeDto>>;

public class GetBetTypesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetBetTypesQuery, IReadOnlyList<BetTypeDto>>
{
    public async Task<IReadOnlyList<BetTypeDto>> Handle(GetBetTypesQuery request, CancellationToken cancellationToken)
    {
        var betTypes = await db.BetTypeConfigs.Where(b => b.IsActive).ToListAsync(cancellationToken);

        return betTypes
            .Select(b => new BetTypeDto(b.Id, b.Code, b.DisplayLabel, b.Category.ToString(), b.Multiplier, b.Faces, b.RequiredTotal))
            .ToList();
    }
}
