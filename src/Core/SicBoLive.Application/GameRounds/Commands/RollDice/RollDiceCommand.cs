using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Services;

namespace SicBoLive.Application.GameRounds.Commands.RollDice;

/// <summary>The dealer's single "roll" button: closes betting, rolls the three dice, and settles every bet.</summary>
public record RollDiceCommand(Guid RoundId) : IRequest<RoundResultDto>;

public class RollDiceCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier)
    : IRequestHandler<RollDiceCommand, RoundResultDto>
{
    public async Task<RoundResultDto> Handle(RollDiceCommand request, CancellationToken cancellationToken)
    {
        var round = await db.GameRounds.Include(r => r.Bets).FirstOrDefaultAsync(r => r.Id == request.RoundId, cancellationToken)
            ?? throw new KeyNotFoundException("دور مورد نظر یافت نشد.");

        var group = await db.Groups.FindAsync([round.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        if (group.DealerId != currentUser.UserId)
            throw new UnauthorizedAccessException("فقط دیلر همین میز می‌تواند تاس بیندازد.");

        if (round.Status == Domain.Enums.RoundStatus.Betting)
            round.CloseBetting();

        await notifier.BettingClosed(round.GroupId, round.Id, cancellationToken);

        var diceRoll = round.RollDice();
        var diceDto = new DiceResultDto(diceRoll.Die1, diceRoll.Die2, diceRoll.Die3, diceRoll.Total, diceRoll.IsTriple);
        await notifier.DiceRolled(round.GroupId, round.Id, diceDto, cancellationToken);

        var betTypeIds = round.Bets.Select(b => b.BetTypeConfigId).Distinct().ToList();
        var betTypes = await db.BetTypeConfigs.Where(bt => betTypeIds.Contains(bt.Id)).ToDictionaryAsync(bt => bt.Id, cancellationToken);

        var walletsByPlayer = await db.Wallets
            .Where(w => w.GroupId == round.GroupId)
            .ToDictionaryAsync(w => w.UserId, cancellationToken);

        // The dealer has their own wallet in the same group ("the house") - lost stakes flow
        // into it so the dealer can see their running take. Created lazily on first payout,
        // same lazy pattern as a player's wallet on JoinGroup.
        if (!walletsByPlayer.TryGetValue(group.DealerId, out var dealerWallet))
        {
            dealerWallet = new Wallet(round.GroupId, group.DealerId);
            db.Wallets.Add(dealerWallet);
            walletsByPlayer[group.DealerId] = dealerWallet;
        }

        var settlements = new List<BetSettlementDto>();
        var dealerBalanceChanged = false;

        foreach (var bet in round.Bets)
        {
            var betType = betTypes[bet.BetTypeConfigId];
            var (won, multiplier) = SicBoPayoutEngine.Evaluate(diceRoll, betType);
            bet.Settle(won, multiplier);

            var wallet = walletsByPlayer[bet.PlayerId];
            if (won)
            {
                var payout = bet.Amount + bet.WinAmount; // return stake + winnings
                wallet.Credit(payout);
                db.TokenTransactions.Add(new TokenTransaction(round.GroupId, wallet.Id, payout, Domain.Enums.TokenTransactionType.BetWon, null, betType.Code));

                // Only the net winnings (bet.WinAmount) are new money changing hands - bet.Amount
                // is just the player's own stake being handed back (it was already debited from
                // their wallet at bet-placement time in PlaceBetCommand, and never touched the
                // dealer's wallet), so it's not re-debited here. The dealer's "house" wallet pays
                // for the winnings out of pocket and is allowed to go negative - it's a purely
                // virtual balance (never real currency, never cashed out), so an IOU there is
                // harmless, unlike letting a *player* wallet go negative.
                if (bet.WinAmount > 0)
                {
                    dealerWallet.DebitAllowingNegative(bet.WinAmount);
                    db.TokenTransactions.Add(new TokenTransaction(
                        round.GroupId, dealerWallet.Id, bet.WinAmount, Domain.Enums.TokenTransactionType.PaidToPlayer,
                        bet.PlayerId, $"برد بازیکن — {betType.Code}"));
                    dealerBalanceChanged = true;
                }
            }
            else
            {
                // The stake was already debited from the player's wallet when the bet was placed
                // (PlaceBetCommand) - on a loss it moves on to the dealer's wallet instead of
                // just disappearing.
                dealerWallet.Credit(bet.Amount);
                db.TokenTransactions.Add(new TokenTransaction(
                    round.GroupId, dealerWallet.Id, bet.Amount, Domain.Enums.TokenTransactionType.ReturnedToDealer,
                    bet.PlayerId, $"باخت بازیکن — {betType.Code}"));
                dealerBalanceChanged = true;
            }

            settlements.Add(new BetSettlementDto(bet.Id, bet.PlayerId, betType.Code, bet.Amount, won, bet.WinAmount, wallet.Balance));
        }

        round.Close();
        await db.SaveChangesAsync(cancellationToken);

        var result = new RoundResultDto(round.GroupId, round.Id, diceDto, settlements);
        await notifier.RoundSettled(result, cancellationToken);

        foreach (var playerId in settlements.Select(s => s.PlayerId).Distinct())
        {
            var wallet = walletsByPlayer[playerId];
            await notifier.WalletUpdated(new WalletBalanceDto(round.GroupId, playerId, wallet.Balance), cancellationToken);
        }

        if (dealerBalanceChanged)
            await notifier.WalletUpdated(new WalletBalanceDto(round.GroupId, group.DealerId, dealerWallet.Balance), cancellationToken);

        return result;
    }
}
