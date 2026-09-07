using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Domain.Seed;

/// <summary>
/// The core paytable read off the physical board image: Big/Small, Odd/Even, Totals 4-17,
/// Single/Double/Specific-triple numbers, Any Triple, the 15 two-number Combinations, the 30
/// specific-double-plus-third-face spots (e.g. 1-1-2, pays 60x), and the 20 three-distinct-face
/// spots (e.g. 1-2-6, pays 30x).
/// </summary>
public static class StandardSicBoBoard
{
    // total -> "1 wins X" multiplier, read directly off the board.
    private static readonly Dictionary<int, decimal> TotalMultipliers = new()
    {
        [4] = 60m, [17] = 60m,
        [5] = 20m, [16] = 20m,
        [6] = 18m, [15] = 18m,
        [7] = 12m, [14] = 12m,
        [8] = 8m, [13] = 8m,
        [9] = 7m, [12] = 7m,
        [10] = 6m, [11] = 6m,
    };

    public static IReadOnlyList<BetTypeConfig> CreateDefaultBetTypes()
    {
        var list = new List<BetTypeConfig>
        {
            new("BIG", "BIG (11-17)", BetCategory.BigSmall, 1m),
            new("SMALL", "SMALL (4-10)", BetCategory.BigSmall, 1m),
            new("ODD", "ODD", BetCategory.OddEven, 1m),
            new("EVEN", "EVEN", BetCategory.OddEven, 1m),
            new("ANY_TRIPLE", "ANY TRIPLE", BetCategory.AnyTriple, 30m),
        };

        foreach (var (total, multiplier) in TotalMultipliers)
            list.Add(new BetTypeConfig($"TOTAL_{total}", total.ToString(), BetCategory.Total, multiplier, requiredTotal: total));

        for (var face = 1; face <= 6; face++)
        {
            list.Add(new BetTypeConfig($"SINGLE_{face}", $"Single {face}", BetCategory.SingleNumber, 1m, faces: new[] { face }));
            list.Add(new BetTypeConfig($"DOUBLE_{face}", $"Double {face}", BetCategory.DoubleNumber, 10m, faces: new[] { face }));
            list.Add(new BetTypeConfig($"TRIPLE_{face}", $"Triple {face}", BetCategory.SpecificTriple, 180m, faces: new[] { face }));
        }

        for (var a = 1; a <= 6; a++)
        {
            for (var b = a + 1; b <= 6; b++)
            {
                list.Add(new BetTypeConfig($"COMBO_{a}_{b}", $"{a} + {b}", BetCategory.Combination, 6m, faces: new[] { a, b }));
            }
        }

        for (var pair = 1; pair <= 6; pair++)
        {
            for (var third = 1; third <= 6; third++)
            {
                if (third == pair) continue;
                list.Add(new BetTypeConfig($"SPECDBL_{pair}_{third}", $"{pair}{pair}{third}", BetCategory.SpecificDouble, 60m,
                    faces: new[] { pair, pair, third }));
            }
        }

        for (var a = 1; a <= 6; a++)
        {
            for (var b = a + 1; b <= 6; b++)
            {
                for (var c = b + 1; c <= 6; c++)
                {
                    list.Add(new BetTypeConfig($"COMBO3_{a}_{b}_{c}", $"{a}{b}{c}", BetCategory.ThreeNumberCombo, 30m,
                        faces: new[] { a, b, c }));
                }
            }
        }

        return list;
    }
}
