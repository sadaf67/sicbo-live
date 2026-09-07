using SicBoLive.Domain.Common;

namespace SicBoLive.Domain.Entities;

/// <summary>
/// Tracks a Telegram-bot signup that hasn't been approved by the admin yet. No ApplicationUser or
/// PlayerSubscription exists while this record is pending - both are created together, atomically,
/// the moment the admin taps "تایید" on the forwarded receipt (see TelegramBotService). This replaces
/// the old flow where /start created the account immediately and only betting was gated behind
/// subscription; now the account itself doesn't exist until the admin has actually reviewed the
/// payment receipt.
///
/// Persisted (not just held in an in-memory dictionary) so an in-flight signup survives a backend
/// restart between "receipt sent" and "admin approved" - this dev box's watchdog restarts the backend
/// automatically after a crash (see tools/watchdog.ps1), and silently losing someone's payment proof
/// mid-flight would be a real support headache for a friends-and-family group.
/// </summary>
public class PendingRegistration : BaseEntity
{
    public long ChatId { get; private set; }
    public string DisplayName { get; private set; } = default!;
    public Guid SubscriptionPlanId { get; private set; }
    public string? ReceiptTelegramFileId { get; private set; }
    public DateTimeOffset? ReceiptSentAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public Guid? ResultUserId { get; private set; }

    private PendingRegistration() { }

    public PendingRegistration(long chatId, string displayName, Guid subscriptionPlanId)
    {
        if (chatId == 0) throw new ArgumentException("چت نامعتبر است.");
        if (subscriptionPlanId == Guid.Empty) throw new ArgumentException("طرح اشتراک نامعتبر است.");

        ChatId = chatId;
        DisplayName = displayName;
        SubscriptionPlanId = subscriptionPlanId;
    }

    public bool IsDecided => ApprovedAt is not null || RejectedAt is not null;

    /// <summary>Lets the player change their mind and pick a different plan before sending a receipt.</summary>
    public void ChangePlan(Guid subscriptionPlanId)
    {
        if (IsDecided) throw new InvalidOperationException("این درخواست قبلاً تصمیم‌گیری شده است.");
        if (ReceiptTelegramFileId is not null) throw new InvalidOperationException("رسید قبلاً ارسال شده است.");
        SubscriptionPlanId = subscriptionPlanId;
    }

    public void AttachReceipt(string telegramFileId)
    {
        if (IsDecided) throw new InvalidOperationException("این درخواست قبلاً تصمیم‌گیری شده است.");
        ReceiptTelegramFileId = telegramFileId;
        ReceiptSentAt = DateTimeOffset.UtcNow;
    }

    public void Approve(Guid resultUserId)
    {
        if (IsDecided) throw new InvalidOperationException("این درخواست قبلاً تصمیم‌گیری شده است.");
        ApprovedAt = DateTimeOffset.UtcNow;
        ResultUserId = resultUserId;
    }

    public void Reject()
    {
        if (IsDecided) throw new InvalidOperationException("این درخواست قبلاً تصمیم‌گیری شده است.");
        RejectedAt = DateTimeOffset.UtcNow;
    }
}
