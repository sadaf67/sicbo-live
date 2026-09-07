using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Groups.Commands.IssueTokens;

/// <summary>Dealer hands (or takes back) tokens for a player. Positive amount = issue, negative = return.</summary>
public record IssueTokensCommand(Guid GroupId, Guid PlayerId, decimal Amount) : IRequest<decimal>;

public class IssueTokensCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier)
    : IRequestHandler<IssueTokensCommand, decimal>
{
    public async Task<decimal> Handle(IssueTokensCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount == 0)
            throw new ArgumentException("مبلغ نمی‌تواند صفر باشد.");

        var group = await db.Groups.FindAsync([request.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        if (group.DealerId != currentUser.UserId)
            throw new UnauthorizedAccessException("فقط دیلر همین میز می‌تواند ژتون صادر کند.");

        var wallet = await db.Wallets.FirstOrDefaultAsync(
            w => w.GroupId == request.GroupId && w.UserId == request.PlayerId, cancellationToken);

        if (wallet is null)
        {
            wallet = new Wallet(request.GroupId, request.PlayerId);
            db.Wallets.Add(wallet);
        }

        var type = request.Amount > 0 ? TokenTransactionType.IssuedByDealer : TokenTransactionType.ReturnedToDealer;

        if (request.Amount > 0)
            wallet.Credit(request.Amount);
        else
            wallet.Debit(-request.Amount);

        db.TokenTransactions.Add(new TokenTransaction(request.GroupId, wallet.Id, Math.Abs(request.Amount), type, currentUser.UserId));

        await db.SaveChangesAsync(cancellationToken);
        await notifier.WalletUpdated(new WalletBalanceDto(request.GroupId, request.PlayerId, wallet.Balance), cancellationToken);

        return wallet.Balance;
    }
}
