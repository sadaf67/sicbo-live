namespace SicBoLive.Application.Common.Models;

public record DiceResultDto(int Die1, int Die2, int Die3, int Total, bool IsTriple);

public record BetSettlementDto(Guid BetId, Guid PlayerId, string BetTypeCode, decimal Amount, bool Won, decimal WinAmount, decimal NewBalance);

public record RoundResultDto(Guid GroupId, Guid RoundId, DiceResultDto Dice, IReadOnlyList<BetSettlementDto> Settlements);

public record RoundStartedDto(Guid GroupId, Guid RoundId, int RoundNumber, DateTimeOffset BettingEndsAt);

public record WalletBalanceDto(Guid GroupId, Guid UserId, decimal Balance);

public record TokenLimitsUpdatedDto(Guid GroupId, decimal MinBetTokens, decimal MaxBetTokens);

// Broadcast to the whole table whenever the dealer changes the per-round betting countdown
// (SetBettingWindowCommand) - only affects rounds started after the change, see that command's doc comment.
public record BettingWindowUpdatedDto(Guid GroupId, int BettingWindowSeconds);
