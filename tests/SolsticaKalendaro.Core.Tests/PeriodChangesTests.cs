using Xunit;

namespace SolsticaKalendaro.Core.Tests;

/// <summary>
/// What a screen for one period of section 9.3 has to be able to say, and what it must not have
/// to invent: where the Supertago falls, and what is different about a year once the period has
/// begun. All of it read off the table, so a wrong row shows up here rather than in a sentence.
/// </summary>
public class PeriodChangesTests
{
    private static ValidityPeriod Period(int firstYear) =>
        ValidityPeriod.Table.Single(p => p.FirstYear == firstYear);

    // ---------- the one structural reform ----------

    [Fact]
    public void EnteringTheReformOf3324NarrowsTheJarmezoAndWidensTheSecondEquinox()
    {
        var changes = Period(3324).ChangesOnEntering!;

        Assert.Equal(
            [(PeriodKind.Jarmezo, 14, 7), (PeriodKind.EkvinoksoII, 7, 14)],
            changes.Blocks);
    }

    [Fact]
    public void EnteringTheReformOf3324MovesExactlyThreeMonths()
    {
        // The Jarmezo loses seven days, so the three months after it begin seven days earlier.
        // Everything from the Ekvinokso II onwards is back where it was, because the two
        // transition blocks still total 28 days between them.
        var changes = Period(3324).ChangesOnEntering!;

        Assert.Equal(
            [(PeriodKind.Sepa, -7), (PeriodKind.Oka, -7), (PeriodKind.Naua, -7)],
            changes.MonthsMoved);
    }

    [Fact]
    public void EnteringTheReformOf3324MovesOneDayBetweenTheFirstTwoSeasons()
    {
        var changes = Period(3324).ChangesOnEntering!;

        Assert.Equal(
            [(Season.First, 88, 89), (Season.Second, 92, 91)],
            changes.Seasons);
    }

    [Fact]
    public void EnteringTheReformOf3324MovesTheSupertagoToTheSecondEquinox()
    {
        var changes = Period(3324).ChangesOnEntering!;

        Assert.Equal((PeriodKind.Tria, 28), changes.SupertagoFrom);
        Assert.Equal((PeriodKind.EkvinoksoII, 14), changes.SupertagoTo);
    }

    [Fact]
    public void NoOtherBoundaryRearrangesTheBlocksOrMovesAMonth()
    {
        // Section 9.4: recalibration is semantic. Only the arrangement of the transition blocks
        // moves a date in a common year, and that happens once in the whole window.
        foreach (var period in ValidityPeriod.Table.Where(p => p.FirstYear != 3324))
        {
            if (period.ChangesOnEntering is not { } changes) continue;

            Assert.Empty(changes.Blocks);
            Assert.Empty(changes.MonthsMoved);
        }
    }

    // ---------- every boundary ----------

    [Fact]
    public void EveryMonthThatMovesMovesEarlier()
    {
        // The interface says "begin N days earlier" and has no other sentence. If the table ever
        // moved a month later, that sentence would be a lie rather than a missing case.
        foreach (var period in ValidityPeriod.Table)
            foreach (var (month, days) in period.ChangesOnEntering?.MonthsMoved ?? [])
                Assert.True(days < 0, $"{period.FirstYear}: {month.Name()} moves {days} days.");
    }

    [Fact]
    public void TheSeasonChangesAreTheDifferenceBetweenTheTwoAllocations()
    {
        foreach (var period in ValidityPeriod.Table)
        {
            if (period.ChangesOnEntering is not { } changes) continue;
            var before = period.Previous!;

            foreach (var season in Enum.GetValues<Season>())
            {
                var stated = changes.Seasons.Where(s => s.Season == season).ToList();

                if (before.Allocation[season] == period.Allocation[season])
                {
                    Assert.Empty(stated);
                    continue;
                }

                var one = Assert.Single(stated);
                Assert.Equal(before.Allocation[season], one.From);
                Assert.Equal(period.Allocation[season], one.To);
            }

            // Days are moved between seasons, never added: the year is still 365 long.
            Assert.True(period.Allocation.IsWellFormed);
            Assert.Equal(changes.Seasons.Sum(s => s.From), changes.Seasons.Sum(s => s.To));
        }
    }

    // ---------- where the Supertago falls ----------

    [Theory]
    [InlineData(2000, PeriodKind.Jarmezo, 7)]
    [InlineData(2096, PeriodKind.EkvinoksoII, 7)]
    [InlineData(3151, PeriodKind.Tria, 28)]
    [InlineData(3324, PeriodKind.EkvinoksoII, 14)]
    [InlineData(4503, PeriodKind.Sesa, 28)]
    [InlineData(7722, PeriodKind.EkvinoksoII, 7)]
    public void TheSupertagoFollowsTheDayTheSeamNames(int firstYear, PeriodKind block, int day)
    {
        Assert.Equal((block, day), Period(firstYear).SupertagoFollows);
    }

    [Fact]
    public void TheSupertagoFollowsTheDayBeforeItsOwnOrdinal()
    {
        foreach (var period in ValidityPeriod.Table)
        {
            var (block, day) = period.SupertagoFollows;
            Assert.Equal(period.SupertagoSeam, period.Layout.CommonOrdinal(block, day));
            Assert.Equal(period.SupertagoOrdinal, period.SupertagoSeam + 1);
        }
    }

    // ---------- the ends of the table ----------

    [Fact]
    public void TheFirstPeriodBeginsNothingAndTheLastLeadsNowhere()
    {
        var first = ValidityPeriod.Table[0];
        var last = ValidityPeriod.Table[^1];

        Assert.Null(first.Previous);
        Assert.Null(first.ChangesOnEntering);
        Assert.Null(last.Next);

        Assert.NotNull(first.Next);
        Assert.NotNull(last.Previous);
    }

    [Fact]
    public void ThePeriodsAreOneChainFromEndToEnd()
    {
        var walked = new List<int>();
        for (var period = ValidityPeriod.Table[0]; period is not null; period = period.Next)
            walked.Add(period.FirstYear);

        Assert.Equal(ValidityPeriod.Table.Select(p => p.FirstYear), walked);

        for (var period = ValidityPeriod.Table[^1]; period.Previous is { } before; period = before)
            Assert.Equal(before.LastYear + 1, period.FirstYear);
    }
}
