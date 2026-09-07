using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;

namespace SicBoLive.Application.Admin.Queries.GetAllGroups;

public record AdminGroupDto(
    Guid GroupId,
    string Name,
    string InviteCode,
    string DealerDisplayName,
    decimal MinBetTokens,
    decimal MaxBetTokens,
    bool IsActive);

public record GetAllGroupsQuery : IRequest<IReadOnlyList<AdminGroupDto>>;

public class GetAllGroupsQueryHandler(IApplicationDbContext db, IUserDirectory userDirectory)
    : IRequestHandler<GetAllGroupsQuery, IReadOnlyList<AdminGroupDto>>
{
    public async Task<IReadOnlyList<AdminGroupDto>> Handle(GetAllGroupsQuery request, CancellationToken cancellationToken)
    {
        var groups = await db.Groups.ToListAsync(cancellationToken);
        var dealerNames = await userDirectory.GetDisplayNamesAsync(groups.Select(g => g.DealerId).Distinct(), cancellationToken);

        return groups
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new AdminGroupDto(
                g.Id,
                g.Name,
                g.InviteCode,
                dealerNames.GetValueOrDefault(g.DealerId, "؟"),
                g.MinBetTokens,
                g.MaxBetTokens,
                g.IsActive))
            .ToList();
    }
}
