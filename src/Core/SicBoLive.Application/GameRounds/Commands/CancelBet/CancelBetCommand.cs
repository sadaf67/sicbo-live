using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.GameRounds.Commands.CancelBet;

/// <summary>Player takes back a chip they placed, before the dealer closes betting (changed their mind).</summary>
public record CancelBetCommand(Guid BetId) : IRequest<Unit>;

public class CancelBetCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier)
    : IRequestHandler<CancelBetCommand, Unit>
{
    public async Task<Unit> Handle(CancelBetCommand request, CancellationToken cancellationToken)
    {
        var bet = await db.Bets.FirstOrDefaultAsync(b => b.Id == request.BetId, cancellationToken)
            ?? throw new KeyNotFoundException("شرط مورد نظر یافت نشد.");

        if (bet.PlayerId != currentUser.UserId)
            throw new UnauthorizedAccessException("شما نمی‌توانید شرط بازیکن دیگری را پس بگیرید.");

        if (bet.Outcome != BetOutcome.Pending)
            throw new InvalidOperationException("این شرط قبلاً تسویه شده و دیگر قابل پس گرفتن نیست.");

        var round = await db.GameRounds.FindAsync([bet.GameRoundId], cancellationToken)
            ?? throw new KeyNotFoundException("دور مورد نظر یافت نشد.");

        round.EnsureBetCancellable();

        var wallet = await db.Wallets.FirstOrDefaultAsync(
            w => w.GroupId == round.GroupId && w.UserId == currentUser.UserId, cancellationToken)
            ?? throw new InvalidOperationException("کیف پول شما در این میز یافت نشد.");

        var betTypeCode = (await db.BetTypeConfigs.FindAsync([bet.BetTypeConfigId], cancellationToken))?.Code;

        wallet.Credit(bet.Amount);
        db.Bets.Remove(bet);
        db.TokenTransactions.Add(new TokenTransaction(
            round.GroupId, wallet.Id, bet.Amount, TokenTransactionType.BetCancelled, currentUser.UserId, betTypeCode));

        await db.SaveChangesAsync(cancellationToken);
        await notifier.WalletUpdated(new WalletBalanceDto(round.GroupId, currentUser.UserId, wallet.Balance), cancellationToken);

        return Unit.Value;
    }
}
