namespace SicBoLive.Domain.Enums;

/// <summary>Matches the sections of the physical Sic Bo board image.</summary>
public enum BetCategory
{
    BigSmall = 0,       // BIG (11-17) / SMALL (4-10), loses on any triple
    OddEven = 1,         // EVEN / ODD, loses on any triple
    Total = 2,           // total of the three dice, 4-17
    SingleNumber = 3,    // one chosen face (1-6), pays per matching die
    DoubleNumber = 4,    // exactly two dice show the chosen face
    SpecificTriple = 5,  // all three dice show the chosen face
    AnyTriple = 6,       // all three dice show the same face, any face
    Combination = 7,     // two chosen faces both appear (e.g. 6-5, 5-4 ...)
    SpecificDouble = 8,  // dice show exactly this pair plus this specific third face (e.g. 1-1-2)
    ThreeNumberCombo = 9 // dice show exactly these three distinct faces, any order (e.g. 1-2-6)
}
