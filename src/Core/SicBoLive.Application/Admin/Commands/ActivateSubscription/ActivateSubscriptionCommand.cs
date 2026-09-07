using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Admin.Commands.ActivateSubscription;

/// <summary>
/// Admin-only: grants (or renews) a player's site-wide permission to place bets, after the
/// player has paid the membership fee outside the app. No payment happens here — this is
/// purely an admin-controlled on/off record tied to an existing <see cref="SubscriptionPlan"/>.
/// </summary>
public record ActivateSubscriptionCommand(Guid UserId, Guid PlanId) : IRequest<Guid>;

public class ActivateSubscriptionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<ActivateSubscriptionCommand, Guid>
{
    public async Task<Guid> Handle(ActivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("فقط ادمین می‌تواند اشتراک کاربر را فعال کند.");

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new KeyNotFoundException("طرح اشتراک مورد نظر یافت نشد.");

        if (!plan.IsActive)
            throw new InvalidOperationException("این طرح اشتراک غیرفعال است.");

        // Note: EF Core's Sqlite provider can't translate DateTimeOffset comparisons (only equality) to SQL,
        // so RevokedAt == null is filtered server-side and the ExpiresAt check is applied after materializing.
        var now = DateTimeOffset.UtcNow;
        var existingUnrevoked = await db.PlayerSubscriptions
            .Where(s => s.UserId == request.UserId && s.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var old in existingUnrevoked.Where(s => s.ExpiresAt >= now))
            old.Revoke();

        var subscription = new PlayerSubscription(request.UserId, plan.Id, currentUser.UserId, plan.GameDurationMinutes);
        db.PlayerSubscriptions.Add(subscription);

        await db.SaveChangesAsync(cancellationToken);
        return subscription.Id;
    }
}
