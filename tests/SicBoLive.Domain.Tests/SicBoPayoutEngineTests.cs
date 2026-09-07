using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;
using SicBoLive.Domain.Seed;
using SicBoLive.Domain.Services;
using SicBoLive.Domain.ValueObjects;
using Xunit;

namespace SicBoLive.Domain.Tests;

public class SicBoPayoutEngineTests
{
    private static BetTypeConfig Find(string code) =>
        StandardSicBoBoard.CreateDefaultBetTypes().Single(b => b.Code == code);

    [Theory]
    [InlineData(3, 4, 4, true)]   // total 11 -> big
    [InlineData(1, 1, 1, false)]  // triple -> big/small both void
    [InlineData(1, 2, 1, false)]  // total 4 -> small, not big
    public void BigSmall_Big_EvaluatesCorrectly(int d1, int d2, int d3, bool expectedWin)
    {
        var roll = new DiceRoll(d1, d2, d3);
        var (won, _) = SicBoPayoutEngine.Evaluate(roll, Find("BIG"));
        Assert.Equal(expectedWin, won);
    }

    [Fact]
    public void AnyTriple_WinsOnAnyMatchingTriple_At30x()
    {
        var roll = new DiceRoll(5, 5, 5);
        var (won, multiplier) = SicBoPayoutEngine.Evaluate(roll, Find("ANY_TRIPLE"));
        Assert.True(won);
        Assert.Equal(30m, multiplier);
    }

    [Fact]
    public void AnyTriple_LosesWhenNotATriple()
    {
        var roll = new DiceRoll(5, 5, 4);
        var (won, _) = SicBoPayoutEngine.Evaluate(roll, Find("ANY_TRIPLE"));
        Assert.False(won);
    }

    [Fact]
    public void SpecificTriple_OnlyWinsForItsOwnFace_At180x()
    {
        var roll = new DiceRoll(6, 6, 6);
        var (wonSix, multiplier) = SicBoPayoutEngine.Evaluate(roll, Find("TRIPLE_6"));
        var (wonFive, _) = SicBoPayoutEngine.Evaluate(roll, Find("TRIPLE_5"));

        Assert.True(wonSix);
        Assert.Equal(180m, multiplier);
        Assert.False(wonFive);
    }

    [Theory]
    [InlineData(4, 60)]
    [InlineData(17, 60)]
    [InlineData(10, 6)]
    [InlineData(9, 7)]
    public void Total_PaysBoardMultiplier_OnExactMatch(int total, decimal expectedMultiplier)
    {
        // pick a matching combination for this total using three dice 1-6
        var roll = FindRollForTotal(total);
        var config = Find($"TOTAL_{total}");
        var (won, multiplier) = SicBoPayoutEngine.Evaluate(roll, config);

        Assert.True(won);
        Assert.Equal(expectedMultiplier, multiplier);
    }

    [Fact]
    public void Total_VoidedByTriple_EvenWhenTripleSumMatchesRequiredTotal()
    {
        // triple 2s sums to 6, which would otherwise match TOTAL_6 - but any triple voids Total
        // bets too, same house rule as BIG/SMALL and ODD/EVEN.
        var roll = new DiceRoll(2, 2, 2);
        var (won, _) = SicBoPayoutEngine.Evaluate(roll, Find("TOTAL_6"));
        Assert.False(won);
    }

    [Theory]
    [InlineData(1, 1, 1, 1, 3)]   // three dice show the face -> 3x
    [InlineData(2, 2, 5, 2, 2)]   // two dice show the face -> 2x
    [InlineData(6, 2, 5, 6, 1)]   // one die shows the face -> 1x
    [InlineData(2, 2, 2, 5, 0)]   // face absent -> loses
    public void SingleNumber_PaysCountOfMatchingDice(int d1, int d2, int d3, int face, int expectedMultiplier)
    {
        var roll = new DiceRoll(d1, d2, d3);
        var (won, multiplier) = SicBoPayoutEngine.Evaluate(roll, Find($"SINGLE_{face}"));

        if (expectedMultiplier == 0)
        {
            Assert.False(won);
        }
        else
        {
            Assert.True(won);
            Assert.Equal(expectedMultiplier, multiplier);
        }
    }

    [Fact]
    public void DoubleNumber_RequiresAtLeastTwoMatchingDice()
    {
        var roll = new DiceRoll(3, 3, 6);
        var (won, multiplier) = SicBoPayoutEngine.Evaluate(roll, Find("DOUBLE_3"));
        Assert.True(won);
        Assert.Equal(10m, multiplier);

        var (lost, _) = SicBoPayoutEngine.Evaluate(roll, Find("DOUBLE_6"));
        Assert.False(lost);
    }

    [Fact]
    public void Combination_WinsWhenBothChosenFacesAppear()
    {
        var roll = new DiceRoll(5, 6, 2);
        var (won, multiplier) = SicBoPayoutEngine.Evaluate(roll, Find("COMBO_5_6"));
        Assert.True(won);
        Assert.Equal(6m, multiplier);

        var (lost, _) = SicBoPayoutEngine.Evaluate(roll, Find("COMBO_1_2"));
        Assert.False(lost);
    }

    [Fact]
    public void SpecificDouble_WinsOnlyForExactPairPlusThird_At60x()
    {
        var roll = new DiceRoll(1, 1, 2);
        var (won, multiplier) = SicBoPayoutEngine.Evaluate(roll, Find("SPECDBL_1_2"));
        Assert.True(won);
        Assert.Equal(60m, multiplier);

        var (lost, _) = SicBoPayoutEngine.Evaluate(new DiceRoll(1, 1, 3), Find("SPECDBL_1_2"));
        Assert.False(lost);

        var (tripleLost, _) = SicBoPayoutEngine.Evaluate(new DiceRoll(1, 1, 1), Find("SPECDBL_1_2"));
        Assert.False(tripleLost);
    }

    [Fact]
    public void ThreeNumberCombo_WinsOnlyWhenAllThreeDistinctFacesPresent_At30x()
    {
        var roll = new DiceRoll(1, 2, 6);
        var (won, multiplier) = SicBoPayoutEngine.Evaluate(roll, Find("COMBO3_1_2_6"));
        Assert.True(won);
        Assert.Equal(30m, multiplier);

        var (lost, _) = SicBoPayoutEngine.Evaluate(new DiceRoll(1, 2, 5), Find("COMBO3_1_2_6"));
        Assert.False(lost);
    }

    [Fact]
    public void Bet_Settle_ComputesWinAmountFromMultiplier()
    {
        var round = new GameRound(Guid.NewGuid(), Guid.NewGuid(), roundNumber: 1, bettingWindowSeconds: 30);
        var bet = round.PlaceBet(Guid.NewGuid(), Find("BIG").Id, amount: 10m);

        bet.Settle(won: true, multiplier: 1m);

        Assert.Equal(BetOutcome.Won, bet.Outcome);
        Assert.Equal(10m, bet.WinAmount);
    }

    private static DiceRoll FindRollForTotal(int total)
    {
        for (var a = 1; a <= 6; a++)
            for (var b = 1; b <= 6; b++)
                for (var c = 1; c <= 6; c++)
                    if (a + b + c == total)
                        return new DiceRoll(a, b, c);

        throw new InvalidOperationException($"No dice combination sums to {total}.");
    }
}
