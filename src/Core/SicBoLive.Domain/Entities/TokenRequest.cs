using SicBoLive.Domain.Common;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Domain.Entities;

/// <summary>
/// A player's request for the table's dealer to hand them a specific amount of tokens.
/// Purely an in-app request/approval record — no payment happens here (tokens are virtual,
/// see the CRITICAL SAFETY CONSTRAINT in CLAUDE.md). Approval simply credits the player's
/// wallet the same way a dealer manually issuing tokens does.
/// </summary>
public class TokenRequest : BaseEntity
{
    public Guid GroupId { get; private set; }
    public Guid PlayerId { get; private set; }
    public decimal Amount { get; private set; }
    public TokenRequestStatus Status { get; private set; } = TokenRequestStatus.Pending;
    public Guid? RespondedByDealerId { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }

    private TokenRequest() { }

    public TokenRequest(Guid groupId, Guid playerId, decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("مقدار درخواستی باید مثبت باشد.");

        GroupId = groupId;
        PlayerId = playerId;
        Amount = amount;
    }

    public void Approve(Guid dealerId)
    {
        if (Status != TokenRequestStatus.Pending)
            throw new InvalidOperationException("این درخواست قبلاً پاسخ داده شده است.");

        Status = TokenRequestStatus.Approved;
        RespondedByDealerId = dealerId;
        RespondedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(Guid dealerId)
    {
        if (Status != TokenRequestStatus.Pending)
            throw new InvalidOperationException("این درخواست قبلاً پاسخ داده شده است.");

        Status = TokenRequestStatus.Rejected;
        RespondedByDealerId = dealerId;
        RespondedAt = DateTimeOffset.UtcNow;
    }
}
