using SicBoLive.Domain.Common;

namespace SicBoLive.Domain.Entities;

/// <summary>
/// Records that the site admin has granted a player permission to place bets (site-wide),
/// after the player paid the membership fee outside the app (cash/bank transfer/etc — this
/// entity never touches real money or a payment gateway, it is purely an admin-controlled
/// on/off record tied to a <see cref="SubscriptionPlan"/> for its duration).
/// </summary>
public class PlayerSubscription : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid SubscriptionPlanId { get; private set; }
    public Guid ActivatedByAdminId { get; private set; }
    public DateTimeOffset ActivatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private PlayerSubscription() { }

    public PlayerSubscription(Guid userId, Guid subscriptionPlanId, Guid activatedByAdminId, int gameDurationMinutes)
    {
        if (userId == Guid.Empty) throw new ArgumentException("کاربر نامعتبر است.");
        if (subscriptionPlanId == Guid.Empty) throw new ArgumentException("طرح اشتراک نامعتبر است.");
        if (gameDurationMinutes <= 0) throw new ArgumentException("مدت زمان باید مثبت باشد.");

        UserId = userId;
        SubscriptionPlanId = subscriptionPlanId;
        ActivatedByAdminId = activatedByAdminId;
        ActivatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = ActivatedAt.AddMinutes(gameDurationMinutes);
    }

    public bool IsCurrentlyActive => RevokedAt is null && DateTimeOffset.UtcNow <= ExpiresAt;

    public void Revoke()
    {
        if (RevokedAt is not null) throw new InvalidOperationException("این اشتراک قبلاً لغو شده است.");
        RevokedAt = DateTimeOffset.UtcNow;
    }
}
