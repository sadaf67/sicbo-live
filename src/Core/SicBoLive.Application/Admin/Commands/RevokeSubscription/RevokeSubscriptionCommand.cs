using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Admin.Commands.RevokeSubscription;

/// <summary>Admin-only: revokes a player's current active site-wide subscription, disabling betting immediately.</summary>
public record RevokeSubscriptionCommand(Guid UserId) : IRequest;

public class RevokeSubscriptionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<RevokeSubscriptionCommand>
{
    public async Task Handle(RevokeSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("فقط ادمین می‌تواند اشتراک کاربر را لغو کند.");

        // Note: EF Core's Sqlite provider can't translate DateTimeOffset comparisons (only equality) to SQL,
        // so RevokedAt == null is filtered server-side and the ExpiresAt check is applied after materializing.
        var now = DateTimeOffset.UtcNow;
        var unrevoked = await db.PlayerSubscriptions
            .Where(s => s.UserId == request.UserId && s.RevokedAt == null)
            .ToListAsync(cancellationToken);
        var activeSubscriptions = unrevoked.Where(s => s.ExpiresAt >= now).ToList();

        if (activeSubscriptions.Count == 0)
            throw new KeyNotFoundException("این کاربر اشتراک فعالی ندارد.");

        foreach (var subscription in activeSubscriptions)
            subscription.Revoke();

        await db.SaveChangesAsync(cancellationToken);
    }
}
