using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;
using SicBoLive.Domain.ValueObjects;

namespace SicBoLive.Domain.Services;

/// <summary>
/// Pure domain logic that decides whether a single bet spot wins against a given dice roll,
/// and what multiplier applies. Stateless and persistence-ignorant by design.
/// </summary>
public static class SicBoPayoutEngine
{
    public static (bool Won, decimal Multiplier) Evaluate(DiceRoll roll, BetTypeConfig config)
    {
        return config.Category switch
        {
            BetCategory.BigSmall => EvaluateBigSmall(roll, config),
            BetCategory.OddEven => EvaluateOddEven(roll, config),
            BetCategory.Total => EvaluateTotal(roll, config),
            BetCategory.SingleNumber => EvaluateSingleNumber(roll, config),
            BetCategory.DoubleNumber => EvaluateDoubleNumber(roll, config),
            BetCategory.SpecificTriple => EvaluateSpecificTriple(roll, config),
            BetCategory.AnyTriple => (roll.IsTriple, config.Multiplier),
            BetCategory.Combination => EvaluateCombination(roll, config),
            BetCategory.SpecificDouble => EvaluateSpecificDouble(roll, config),
            BetCategory.ThreeNumberCombo => EvaluateThreeNumberCombo(roll, config),
            _ => throw new ArgumentOutOfRangeException(nameof(config), $"Unhandled bet category '{config.Category}'.")
        };
    }

    // BIG (11-17) / SMALL (4-10) — any triple voids both, matching the board's house rule.
    private static (bool, decimal) EvaluateBigSmall(DiceRoll roll, BetTypeConfig config)
    {
        if (roll.IsTriple) return (false, 0m);
        var isBig = config.Code == "BIG";
        var won = isBig ? roll.Total is >= 11 and <= 17 : roll.Total is >= 4 and <= 10;
        return (won, config.Multiplier);
    }

    private static (bool, decimal) EvaluateOddEven(DiceRoll roll, BetTypeConfig config)
    {
        if (roll.IsTriple) return (false, 0m);
        var isOdd = config.Code == "ODD";
        var won = isOdd ? roll.Total % 2 == 1 : roll.Total % 2 == 0;
        return (won, config.Multiplier);
    }

    // Same house rule as BIG/SMALL and ODD/EVEN: any triple voids Total bets too, even ones whose
    // required total a triple could technically produce (e.g. triple 2s = 6 would otherwise match
    // a TOTAL_6 bet) - a triple always resolves via the triple-specific bets (AnyTriple /
    // SpecificTriple) only, never a Total bet.
    private static (bool, decimal) EvaluateTotal(DiceRoll roll, BetTypeConfig config)
    {
        if (roll.IsTriple) return (false, 0m);
        var won = config.RequiredTotal.HasValue && roll.Total == config.RequiredTotal.Value;
        return (won, config.Multiplier);
    }

    // "1 to 1 on one die, 2 to 1 on two dice, 3 to 1 on three dice" (board footer).
    private static (bool, decimal) EvaluateSingleNumber(DiceRoll roll, BetTypeConfig config)
    {
        var face = config.Faces[0];
        var count = roll.CountOf(face);
        return count > 0 ? (true, config.Multiplier * count) : (false, 0m);
    }

    private static (bool, decimal) EvaluateDoubleNumber(DiceRoll roll, BetTypeConfig config)
    {
        var face = config.Faces[0];
        var won = roll.CountOf(face) >= 2;
        return (won, config.Multiplier);
    }

    private static (bool, decimal) EvaluateSpecificTriple(DiceRoll roll, BetTypeConfig config)
    {
        var face = config.Faces[0];
        var won = roll.IsTriple && roll.Die1 == face;
        return (won, config.Multiplier);
    }

    private static (bool, decimal) EvaluateCombination(DiceRoll roll, BetTypeConfig config)
    {
        var faceA = config.Faces[0];
        var faceB = config.Faces[1];
        var won = roll.Contains(faceA) && roll.Contains(faceB);
        return (won, config.Multiplier);
    }

    // e.g. faces [1,1,2] — the roll must be exactly two 1s and one 2.
    private static (bool, decimal) EvaluateSpecificDouble(DiceRoll roll, BetTypeConfig config)
    {
        var pair = config.Faces[0];
        var third = config.Faces[2];
        var won = roll.CountOf(pair) == 2 && roll.CountOf(third) == 1;
        return (won, config.Multiplier);
    }

    // e.g. faces [1,2,6] — the roll must be exactly these three distinct faces, any order.
    private static (bool, decimal) EvaluateThreeNumberCombo(DiceRoll roll, BetTypeConfig config)
    {
        var won = roll.Contains(config.Faces[0]) && roll.Contains(config.Faces[1]) && roll.Contains(config.Faces[2]);
        return (won, config.Multiplier);
    }
}
