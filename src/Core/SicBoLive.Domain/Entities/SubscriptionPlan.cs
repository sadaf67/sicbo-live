using SicBoLive.Domain.Common;

namespace SicBoLive.Domain.Entities;

/// <summary>Admin-defined, dynamically editable plan: what a player pays to enter and how long they may play.</summary>
public class SubscriptionPlan : BaseEntity
{
    public string Name { get; private set; } = default!;
    public decimal EntryTokenAmount { get; private set; }
    public int GameDurationMinutes { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public bool IsActive { get; private set; } = true;

    private SubscriptionPlan() { }

    public SubscriptionPlan(string name, decimal entryTokenAmount, int gameDurationMinutes, decimal discountPercent = 0)
    {
        if (entryTokenAmount < 0) throw new ArgumentException("مبلغ ورودی نمی‌تواند منفی باشد.");
        if (gameDurationMinutes <= 0) throw new ArgumentException("مدت زمان باید مثبت باشد.");
        if (discountPercent is < 0 or > 100) throw new ArgumentException("تخفیف باید بین ۰ تا ۱۰۰ باشد.");

        Name = name;
        EntryTokenAmount = entryTokenAmount;
        GameDurationMinutes = gameDurationMinutes;
        DiscountPercent = discountPercent;
    }

    public decimal EffectiveEntryAmount() => EntryTokenAmount * (1 - DiscountPercent / 100m);

    public void Update(decimal entryTokenAmount, int gameDurationMinutes, decimal discountPercent)
    {
        if (entryTokenAmount < 0) throw new ArgumentException("مبلغ ورودی نمی‌تواند منفی باشد.");
        if (gameDurationMinutes <= 0) throw new ArgumentException("مدت زمان باید مثبت باشد.");
        if (discountPercent is < 0 or > 100) throw new ArgumentException("تخفیف باید بین ۰ تا ۱۰۰ باشد.");

        EntryTokenAmount = entryTokenAmount;
        GameDurationMinutes = gameDurationMinutes;
        DiscountPercent = discountPercent;
    }

    public void Deactivate() => IsActive = false;
}
