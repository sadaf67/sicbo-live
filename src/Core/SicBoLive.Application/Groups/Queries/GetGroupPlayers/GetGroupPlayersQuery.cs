using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;

namespace SicBoLive.Application.Groups.Queries.GetGroupPlayers;

public record GroupPlayerDto(Guid UserId, string DisplayName, decimal Balance);

public record GetGroupPlayersQuery(Guid GroupId) : IRequest<IReadOnlyList<GroupPlayerDto>>;

public class GetGroupPlayersQueryHandler(IApplicationDbContext db, IUserDirectory userDirectory)
    : IRequestHandler<GetGroupPlayersQuery, IReadOnlyList<GroupPlayerDto>>
{
    public async Task<IReadOnlyList<GroupPlayerDto>> Handle(GetGroupPlayersQuery request, CancellationToken cancellationToken)
    {
        var wallets = await db.Wallets
            .Where(w => w.GroupId == request.GroupId)
            .ToListAsync(cancellationToken);

        var names = await userDirectory.GetDisplayNamesAsync(wallets.Select(w => w.UserId), cancellationToken);

        return wallets
            .Select(w => new GroupPlayerDto(w.UserId, names.GetValueOrDefault(w.UserId, "Unknown"), w.Balance))
            .OrderBy(p => p.DisplayName)
            .ToList();
    }
}
