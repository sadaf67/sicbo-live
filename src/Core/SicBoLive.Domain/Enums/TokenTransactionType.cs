namespace SicBoLive.Domain.Enums;

public enum TokenTransactionType
{
    IssuedByDealer = 0,
    ReturnedToDealer = 1,
    BetPlaced = 2,
    BetWon = 3,
    BetPushed = 4,
    BetCancelled = 5,
    /// <summary>Dealer's wallet debited for the net winnings (stake excluded) paid out to a player on a won bet.</summary>
    PaidToPlayer = 6
}
