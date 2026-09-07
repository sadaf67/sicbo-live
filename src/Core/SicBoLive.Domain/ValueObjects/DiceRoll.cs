namespace SicBoLive.Domain.ValueObjects;

public sealed record DiceRoll(int Die1, int Die2, int Die3)
{
    public static DiceRoll Random(Random? rng = null)
    {
        rng ??= System.Random.Shared;
        return new DiceRoll(rng.Next(1, 7), rng.Next(1, 7), rng.Next(1, 7));
    }

    public IReadOnlyList<int> Values => new[] { Die1, Die2, Die3 };

    public int Total => Die1 + Die2 + Die3;

    public bool IsTriple => Die1 == Die2 && Die2 == Die3;

    public int CountOf(int face) => Values.Count(v => v == face);

    public bool Contains(int face) => Die1 == face || Die2 == face || Die3 == face;
}
