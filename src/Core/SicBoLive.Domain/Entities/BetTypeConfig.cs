using SicBoLive.Domain.Common;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Domain.Entities;

/// <summary>
/// Data-driven description of one betting spot on the board (e.g. "BIG", "TOTAL_10", "SINGLE_4").
/// Keeping the paytable as data lets new spots from the board be added later without touching the payout engine.
/// </summary>
public class BetTypeConfig : BaseEntity
{
    /// <summary>Stable machine key referenced by bets, e.g. "BIG", "TOTAL_10", "SINGLE_4", "COMBO_5_6".</summary>
    public string Code { get; private set; } = default!;
    public string DisplayLabel { get; private set; } = default!;
    public BetCategory Category { get; private set; }

    /// <summary>"1 wins X" payout multiplier for this spot (excludes the returned stake).</summary>
    public decimal Multiplier { get; private set; }

    /// <summary>Die faces relevant to this spot (1-6). Empty for Big/Small/Odd/Even/AnyTriple.</summary>
    public IReadOnlyList<int> Faces { get; private set; } = Array.Empty<int>();

    /// <summary>Required total (4-17) for Total-category spots; null otherwise.</summary>
    public int? RequiredTotal { get; private set; }

    public bool IsActive { get; private set; } = true;

    private BetTypeConfig() { }

    public BetTypeConfig(string code, string displayLabel, BetCategory category, decimal multiplier,
        IReadOnlyList<int>? faces = null, int? requiredTotal = null)
    {
        Code = code;
        DisplayLabel = displayLabel;
        Category = category;
        Multiplier = multiplier;
        Faces = faces ?? Array.Empty<int>();
        RequiredTotal = requiredTotal;
    }

    public void Deactivate() => IsActive = false;
}
