using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;

namespace SicBoLive.Application.Groups.Queries.GetMyGroups;

public record MyGroupDto(Guid GroupId, string Name, string InviteCode, bool IsActive);

public record GetMyGroupsQuery : IRequest<IReadOnlyList<MyGroupDto>>;

public class GetMyGroupsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyGroupsQuery, IReadOnlyList<MyGroupDto>>
{
    public async Task<IReadOnlyList<MyGroupDto>> Handle(GetMyGroupsQuery request, CancellationToken cancellationToken)
    {
        var groups = await db.Groups
            .Where(g => g.DealerId == currentUser.UserId)
            .Select(g => new { g.Id, g.Name, g.InviteCode, g.IsActive, g.CreatedAt })
            .ToListAsync(cancellationToken);

        return groups
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new MyGroupDto(g.Id, g.Name, g.InviteCode, g.IsActive))
            .ToList();
    }
}
