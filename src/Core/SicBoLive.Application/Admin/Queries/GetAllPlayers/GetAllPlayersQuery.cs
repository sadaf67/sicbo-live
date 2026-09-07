using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Admin.Queries.GetAllPlayers;

public record PlayerAdminDto(
    Guid UserId,
    string DisplayName,
    string Email,
    bool HasActiveSubscription,
    Guid? ActiveSubscriptionId,
    Guid? ActiveSubscriptionPlanId,
    DateTimeOffset? ActiveSubscriptionExpiresAt,
    bool HasVerificationPhoto);

public record GetAllPlayersQuery : IRequest<IReadOnlyList<PlayerAdminDto>>;

public class GetAllPlayersQueryHandler(IApplicationDbContext db, IUserDirectory userDirectory, ICurrentUserService currentUser)
    : IRequestHandler<GetAllPlayersQuery, IReadOnlyList<PlayerAdminDto>>
{
    public async Task<IReadOnlyList<PlayerAdminDto>> Handle(GetAllPlayersQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("فقط ادمین می‌تواند فهرست کاربران را ببیند.");

        var players = await userDirectory.GetPlayersAsync(cancellationToken);
        var subscriptions = await db.PlayerSubscriptions
            .Where(s => s.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var activeByUser = subscriptions
            .Where(s => s.ExpiresAt >= now)
            .GroupBy(s => s.UserId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.ExpiresAt).First());

        return players
            .Select(p =>
            {
                activeByUser.TryGetValue(p.UserId, out var active);
                return new PlayerAdminDto(
                    p.UserId,
                    p.DisplayName,
                    p.Email,
                    active is not null,
                    active?.Id,
                    active?.SubscriptionPlanId,
                    active?.ExpiresAt,
                    p.HasVerificationPhoto);
            })
            .ToList();
    }
}
