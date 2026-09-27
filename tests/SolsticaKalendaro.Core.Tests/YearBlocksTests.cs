using Xunit;

namespace SolsticaKalendaro.Core.Tests;

/// <summary>
/// <see cref="SolsticaCalendar.Blocks"/>: the year as an interface has to offer it — every block
/// that exists, in the order it comes, and no day that is not there. Asserted over every period
/// of the table rather than over a few years, because what changes between them is exactly this.
/// </summary>
public class YearBlocksTests
{
    [Theory]
    [MemberData(nameof(OneYearOfEachKindPerPeriod))]
    public void TheBlocksAccountForEveryDayOfTheYear(int year)
    {
        int days = SolsticaCalendar.Blocks(year).Sum(b => b.Days);
        Assert.Equal(SolsticaCalendar.DaysInYear(year), days);
    }

    [Theory]
    [MemberData(nameof(OneYearOfEachKindPerPeriod))]
    public void ThereIsASupertagoInALeapYearAndInNoOther(int year)
    {
        int supertagoj = SolsticaCalendar.Blocks(year).Count(b => b.Kind == PeriodKind.Supertago);
        Assert.Equal(SolsticaCalendar.IsLeapYear(year) ? 1 : 0, supertagoj);
    }

    [Theory]
    [MemberData(nameof(ALeapYearOfEachPeriod))]
    public void TheSupertagoFollowsTheBlockHoldingTheLastDayBeforeIt(int year)
    {
        // The seam is a common-year ordinal, so the block it lands in is the one the Supertago
        // interrupts or closes. Either way the Supertago comes next, which is the order an
        // interface has to offer: in the 2000 period it splits the Jarmezo, in the 7722 period
        // the Ekvinokso II, and in the other nine it sits on a block seam.
        var period = ValidityPeriod.For(year);
        var before = period.Layout.FromCommonOrdinal(period.SupertagoSeam).Period;

        var blocks = SolsticaCalendar.Blocks(year);
        int at = blocks.ToList().FindIndex(b => b.Kind == PeriodKind.Supertago);

        Assert.True(at > 0, $"{year}: the Supertago is not among the blocks, or opens the year.");
        Assert.Equal(before, blocks[at - 1].Kind);
        Assert.Equal(period.SupertagoSeam + 1, SolsticaCalendar.DayOfYear(SolsticaDate.Supertago(year)));
    }

    [Theory]
    [MemberData(nameof(OneYearOfEachKindPerPeriod))]
    public void TheTransitionBlocksCarryTheArrangementOfTheirPeriod(int year)
    {
        var expected = year < ValidityPeriod.StructuralReformYear
            ? BlockWidths.SevenFourteenSeven
            : BlockWidths.SevenSevenFourteen;

        var blocks = SolsticaCalendar.Blocks(year).ToDictionary(b => b.Kind, b => b.Days);

        Assert.Equal(expected.EkvinoksoI, blocks[PeriodKind.EkvinoksoI]);
        Assert.Equal(expected.Jarmezo, blocks[PeriodKind.Jarmezo]);
        Assert.Equal(expected.EkvinoksoII, blocks[PeriodKind.EkvinoksoII]);
    }

    [Theory]
    [MemberData(nameof(OneYearOfEachKindPerPeriod))]
    public void EveryDayTheBlocksOfferIsADayOfThatYear(int year)
    {
        int seen = 0;

        foreach (var (kind, days) in SolsticaCalendar.Blocks(year))
            for (int day = 1; day <= days; day++)
            {
                var date = new SolsticaDate(year, kind, day);

                Assert.True(date.IsValid, $"{date} is offered but does not exist.");
                Assert.Equal(date, SolsticaCalendar.FromDayOfYear(year, SolsticaCalendar.DayOfYear(date)));
                seen++;
            }

        // And no day of the year is left unoffered: the count settles it, the round trip above
        // having shown that no two of them are the same day.
        Assert.Equal(SolsticaCalendar.DaysInYear(year), seen);
    }

    // ---------- the years ----------

    /// <summary>A common year and a leap year from every period of the table.</summary>
    public static TheoryData<int> OneYearOfEachKindPerPeriod =>
        [.. ValidityPeriod.Table.SelectMany(p => new[] { FirstCommonYear(p), FirstLeapYear(p) })];

    public static TheoryData<int> ALeapYearOfEachPeriod =>
        [.. ValidityPeriod.Table.Select(FirstLeapYear)];

    private static int FirstLeapYear(ValidityPeriod period) =>
        Enumerable.Range(period.FirstYear, period.LastYear - period.FirstYear + 1)
            .First(SolsticaCalendar.IsLeapYear);

    private static int FirstCommonYear(ValidityPeriod period) =>
        Enumerable.Range(period.FirstYear, period.LastYear - period.FirstYear + 1)
            .First(y => !SolsticaCalendar.IsLeapYear(y));
}
