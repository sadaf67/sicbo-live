using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Groups.Queries.GetPendingTokenRequests;

public record GetPendingTokenRequestsQuery(Guid GroupId) : IRequest<IReadOnlyList<TokenRequestDto>>;

public class GetPendingTokenRequestsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser, IUserDirectory userDirectory)
    : IRequestHandler<GetPendingTokenRequestsQuery, IReadOnlyList<TokenRequestDto>>
{
    public async Task<IReadOnlyList<TokenRequestDto>> Handle(GetPendingTokenRequestsQuery request, CancellationToken cancellationToken)
    {
        var group = await db.Groups.FindAsync([request.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        if (group.DealerId != currentUser.UserId)
            throw new UnauthorizedAccessException("فقط دیلر همین میز می‌تواند درخواست‌های ژتون را ببیند.");

        // SQLite cannot translate ORDER BY on a DateTimeOffset column - materialize first, then sort in-memory.
        var requests = await db.TokenRequests
            .Where(r => r.GroupId == request.GroupId && r.Status == TokenRequestStatus.Pending)
            .ToListAsync(cancellationToken);

        var names = await userDirectory.GetDisplayNamesAsync(requests.Select(r => r.PlayerId), cancellationToken);

        return requests
            .OrderBy(r => r.CreatedAt)
            .Select(r => new TokenRequestDto(
                r.Id, r.GroupId, r.PlayerId, names.GetValueOrDefault(r.PlayerId, "?"), r.Amount,
                r.Status.ToString(), r.CreatedAt))
            .ToList();
    }
}
