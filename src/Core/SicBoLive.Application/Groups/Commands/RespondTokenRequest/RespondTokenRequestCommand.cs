using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Groups.Commands.RespondTokenRequest;

/// <summary>Dealer approves (credits the wallet) or rejects a player's pending token request.</summary>
public record RespondTokenRequestCommand(Guid RequestId, bool Approve) : IRequest;

public class RespondTokenRequestCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier)
    : IRequestHandler<RespondTokenRequestCommand>
{
    public async Task Handle(RespondTokenRequestCommand request, CancellationToken cancellationToken)
    {
        var tokenRequest = await db.TokenRequests.FindAsync([request.RequestId], cancellationToken)
            ?? throw new KeyNotFoundException("درخواست مورد نظر یافت نشد.");

        if (tokenRequest.Status != TokenRequestStatus.Pending)
            throw new InvalidOperationException("این درخواست قبلاً پاسخ داده شده است.");

        var group = await db.Groups.FindAsync([tokenRequest.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        if (group.DealerId != currentUser.UserId)
            throw new UnauthorizedAccessException("فقط دیلر همین میز می‌تواند به درخواست ژتون پاسخ دهد.");

        decimal newBalance = 0;

        if (request.Approve)
        {
            var wallet = await db.Wallets.FirstOrDefaultAsync(
                w => w.GroupId == tokenRequest.GroupId && w.UserId == tokenRequest.PlayerId, cancellationToken)
                ?? throw new InvalidOperationException("کیف پول این بازیکن یافت نشد.");

            wallet.Credit(tokenRequest.Amount);
            db.TokenTransactions.Add(new TokenTransaction(
                tokenRequest.GroupId, wallet.Id, tokenRequest.Amount, TokenTransactionType.IssuedByDealer,
                currentUser.UserId, "تأیید درخواست بازیکن"));
            tokenRequest.Approve(currentUser.UserId);
            newBalance = wallet.Balance;
        }
        else
        {
            tokenRequest.Reject(currentUser.UserId);
        }

        await db.SaveChangesAsync(cancellationToken);

        await notifier.TokenRequestResolved(new TokenRequestDto(
            tokenRequest.Id, tokenRequest.GroupId, tokenRequest.PlayerId, string.Empty,
            tokenRequest.Amount, tokenRequest.Status.ToString(), tokenRequest.CreatedAt), cancellationToken);

        if (request.Approve)
            await notifier.WalletUpdated(new WalletBalanceDto(tokenRequest.GroupId, tokenRequest.PlayerId, newBalance), cancellationToken);
    }
}
