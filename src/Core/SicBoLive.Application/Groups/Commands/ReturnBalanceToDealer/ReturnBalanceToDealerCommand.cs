using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Groups.Commands.ReturnBalanceToDealer;

/// <summary>
/// A player hands their entire remaining balance in this group back to the dealer before leaving
/// the table - e.g. from a "خروج" (exit) confirmation on the client. Mirrors the two-sided transfer
/// RollDiceCommand already does for lost bet stakes: debit the player's wallet, credit the dealer's
/// wallet with the same amount, one TokenTransaction, notify both wallets over SignalR.
/// </summary>
public record ReturnBalanceToDealerCommand(Guid GroupId) : IRequest<decimal>;

public class ReturnBalanceToDealerCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier)
    : IRequestHandler<ReturnBalanceToDealerCommand, decimal>
{
    public async Task<decimal> Handle(ReturnBalanceToDealerCommand request, CancellationToken cancellationToken)
    {
        var group = await db.Groups.FindAsync([request.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        if (group.DealerId == currentUser.UserId)
            throw new UnauthorizedAccessException("دیلر نمی‌تواند موجودی خودش را به خودش برگرداند.");

        var playerWallet = await db.Wallets.FirstOrDefaultAsync(
            w => w.GroupId == request.GroupId && w.UserId == currentUser.UserId, cancellationToken);

        var amount = playerWallet?.Balance ?? 0m;
        if (playerWallet is null || amount <= 0)
            return 0m; // nothing to return - let the client proceed to leave without an error

        var dealerWallet = await db.Wallets.FirstOrDefaultAsync(
            w => w.GroupId == request.GroupId && w.UserId == group.DealerId, cancellationToken);

        if (dealerWallet is null)
        {
            dealerWallet = new Wallet(request.GroupId, group.DealerId);
            db.Wallets.Add(dealerWallet);
        }

        playerWallet.Debit(amount);
        dealerWallet.Credit(amount);

        db.TokenTransactions.Add(new TokenTransaction(
            request.GroupId, playerWallet.Id, amount, TokenTransactionType.ReturnedToDealer,
            currentUser.UserId, "بازیکن قبل از خروج ژتون باقی‌مانده را به دیلر بازگرداند"));

        await db.SaveChangesAsync(cancellationToken);

        await notifier.WalletUpdated(new WalletBalanceDto(request.GroupId, currentUser.UserId, playerWallet.Balance), cancellationToken);
        await notifier.WalletUpdated(new WalletBalanceDto(request.GroupId, group.DealerId, dealerWallet.Balance), cancellationToken);

        return playerWallet.Balance;
    }
}
