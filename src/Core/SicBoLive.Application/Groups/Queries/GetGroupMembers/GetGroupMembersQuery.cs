using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;

namespace SicBoLive.Application.Groups.Queries.GetGroupMembers;

public record GroupMemberDto(Guid UserId, string DisplayName, bool IsDealer);

/// <summary>
/// Dealer + every player holding a wallet in the table, for the private-chat member picker.
/// Unlike GetGroupPlayersQuery (dealer-only, includes Balance for the token-issuing panel), this
/// is visible to everyone at the table and always includes the dealer as a possible DM target.
/// </summary>
public record GetGroupMembersQuery(Guid GroupId) : IRequest<IReadOnlyList<GroupMemberDto>>;

public class GetGroupMembersQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser, IUserDirectory userDirectory)
    : IRequestHandler<GetGroupMembersQuery, IReadOnlyList<GroupMemberDto>>
{
    public async Task<IReadOnlyList<GroupMemberDto>> Handle(GetGroupMembersQuery request, CancellationToken cancellationToken)
    {
        var group = await db.Groups.FindAsync([request.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        var playerIds = await db.Wallets
            .Where(w => w.GroupId == request.GroupId)
            .Select(w => w.UserId)
            .ToListAsync(cancellationToken);

        var allIds = playerIds.Append(group.DealerId).Distinct().ToList();
        var names = await userDirectory.GetDisplayNamesAsync(allIds, cancellationToken);

        return allIds
            .Where(id => id != currentUser.UserId) // exclude self - nothing to privately message yourself
            .Select(id => new GroupMemberDto(id, names.GetValueOrDefault(id, "Unknown"), id == group.DealerId))
            .OrderByDescending(m => m.IsDealer)
            .ThenBy(m => m.DisplayName)
            .ToList();
    }
}
