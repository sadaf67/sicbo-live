using SicBoLive.Domain.Common;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Domain.Entities;

/// <summary>Audit trail entry for every token movement (dealer issuance/return, bet stake, payout).</summary>
public class TokenTransaction : BaseEntity
{
    public Guid GroupId { get; private set; }
    public Guid WalletId { get; private set; }
    public decimal Amount { get; private set; }
    public TokenTransactionType Type { get; private set; }
    public Guid? PerformedByUserId { get; private set; }
    public string? Note { get; private set; }

    private TokenTransaction() { }

    public TokenTransaction(Guid groupId, Guid walletId, decimal amount, TokenTransactionType type, Guid? performedByUserId, string? note = null)
    {
        GroupId = groupId;
        WalletId = walletId;
        Amount = amount;
        Type = type;
        PerformedByUserId = performedByUserId;
        Note = note;
    }
}
