using Xunit;

namespace SolsticaKalendaro.Core.Tests;

public class YearOutlineTests
{
    private static readonly SolsticaCalendar Cal = new(SolsticaEpoch.Expository2026);

    // ---------- shape ----------

    [Theory]
    [MemberData(nameof(SampleYears))]
    public void EveryYearIsFiftyTwoWeeksAndItsExtraWeeklyDays(int year)
    {
        var rows = Cal.Outline(year);

        Assert.Equal(52, rows.OfType<WeekRow>().Count());
        Assert.Equal(SolsticaCalendar.IsLeapYear(year) ? 2 : 1, rows.OfType<ExtraWeeklyRow>().Count());
        Assert.All(rows.OfType<WeekRow>(), w => Assert.Equal(7, w.Days.Count));
    }

    [Theory]
    [MemberData(nameof(SampleYears))]
    public void EveryYearOpensFifteenSections(int year)
    {
        // Twelve months and three transition blocks. The Jarfino is the sixteenth block of
        // the year but opens nothing, and neither does the Supertago.
        var headers = Cal.Outline(year).OfType<SectionHeader>().Select(h => h.Block);
        var expected = YearLayout.Sequence.Where(p => p != PeriodKind.Jarfino);

        Assert.Equal(expected, headers);
        Assert.Equal(15, expected.Count());
    }

    [Theory]
    [MemberData(nameof(SampleYears))]
    public void EverySectionHeaderOpensItsOwnBlock(int year)
    {
        // Order and count are not enough: a header has to sit immediately before the first
        // week of the block it names. Nothing comes between them, not even a Supertago,
        // which always falls at a week seam and so never lands just after a header.
        var rows = Cal.Outline(year);
        for (int i = 0; i < rows.Count; i++)
            if (rows[i] is SectionHeader header)
                Assert.Equal(header.Block, Assert.IsType<WeekRow>(rows[i + 1]).Block);
    }

    [Theory]
    [MemberData(nameof(SampleYears))]
    public void TheDaysAreTheOrdinalsOfTheYearInOrder(int year)
    {
        var ordinals = DaysOf(Cal.Outline(year)).Select(SolsticaCalendar.DayOfYear);
        Assert.Equal(Enumerable.Range(1, SolsticaCalendar.DaysInYear(year)), ordinals);
    }

    [Theory]
    [MemberData(nameof(SampleYears))]
    public void EveryWeekRunsMondayToSunday(int year)
    {
        foreach (var week in Cal.Outline(year).OfType<WeekRow>())
        {
            Assert.All(week.Days, d => Assert.Equal(week.Block, d.Date.Period));
            Assert.Equal(
                [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
                 DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday],
                week.Days.Select(d => SolsticaCalendar.WeekDay(d.Date)));
        }
    }

    [Theory]
    [MemberData(nameof(SampleYears))]
    public void TheJarfinoClosesTheYear(int year)
    {
        var last = Assert.IsType<ExtraWeeklyRow>(Cal.Outline(year)[^1]);
        Assert.Equal(SolsticaDate.Jarfino(year), last.Day.Date);
    }

    // ---------- the Supertago sits where its period puts it ----------

    [Fact]
    public void TheSupertagoFallsAtItsPeriodsSeamInEveryPeriod()
    {
        foreach (var period in ValidityPeriod.Table)
        {
            int year = LeapYearIn(period);
            var rows = Cal.Outline(year);

            int at = SupertagoAt(rows);
            Assert.True(at >= 0, $"{year}: no Supertago row.");

            // It closes a week rather than splitting one: the seam is a multiple of seven.
            var before = Assert.IsType<WeekRow>(rows[at - 1]);
            Assert.Equal(period.SupertagoSeam, SolsticaCalendar.DayOfYear(before.Days[^1].Date));

            // And the block it interrupts is the one the period's layout says it interrupts.
            var (block, _) = period.Layout.FromCommonOrdinal(period.SupertagoSeam);
            Assert.Equal(block, before.Block);

            // The seam is a block boundary in nine of the eleven periods, and mid-block in
            // the other two, so what follows the Supertago is not always the same kind of
            // row. Let the period's layout say which it should be.
            var (after, _) = period.Layout.FromCommonOrdinal(period.SupertagoSeam + 1);
            if (after == block)
                Assert.Equal(block, Assert.IsType<WeekRow>(rows[at + 1]).Block);
            else
                Assert.Equal(after, Assert.IsType<SectionHeader>(rows[at + 1]).Block);
        }
    }

    [Fact]
    public void TheSupertagoCarriesItsOwnGregorianDateAndSeason()
    {
        // A whole day, not just a name: the band is drawn with a Gregorian date on it, and
        // the seasonal stripe runs through it. In the current period it falls inside the
        // Jarmezo, in the third season, immediately after 7 Jarmezo.
        var rows = Cal.Outline(2028);
        var supertago = Assert.IsType<ExtraWeeklyRow>(rows[SupertagoAt(rows)]).Day;

        Assert.Equal(Season.Third, supertago.Season);
        Assert.Equal(
            Cal.ToGregorian(new SolsticaDate(2028, PeriodKind.Jarmezo, 7)).AddDays(1),
            supertago.Gregorian);
        Assert.True(supertago.IsFestivity);
    }

    [Theory]
    [MemberData(nameof(SampleYears))]
    public void TheJarfinoCarriesItsOwnDayToo(int year)
    {
        var jarfino = Assert.IsType<ExtraWeeklyRow>(Cal.Outline(year)[^1]).Day;

        Assert.Equal(Season.Fourth, jarfino.Season);
        Assert.True(jarfino.IsFestivity);
        Assert.Equal(Cal.CanConvert(jarfino.Date) ? Cal.ToGregorian(jarfino.Date) : null, jarfino.Gregorian);
    }

    [Fact]
    public void InTheCurrentPeriodTheSupertagoInterruptsTheJarmezo()
    {
        var rows = Cal.Outline(2028);
        int at = SupertagoAt(rows);

        Assert.Equal(PeriodKind.Jarmezo, Assert.IsType<WeekRow>(rows[at - 1]).Block);
        Assert.Equal(PeriodKind.Jarmezo, Assert.IsType<WeekRow>(rows[at + 1]).Block); // no new section
    }

    [Fact]
    public void InTheThirdPeriodTheSupertagoFallsBetweenTwoBlocks()
    {
        // 3151-3323 moves it to the seam before the Ekvinokso I, so it lands between two
        // blocks rather than inside one: a section header follows it, and it opened none.
        var rows = Cal.Outline(LeapYearIn(ValidityPeriod.Table[2]));
        int at = SupertagoAt(rows);

        Assert.Equal(PeriodKind.Tria, Assert.IsType<WeekRow>(rows[at - 1]).Block);
        Assert.Equal(PeriodKind.EkvinoksoI, Assert.IsType<SectionHeader>(rows[at + 1]).Block);
    }

    // ---------- what each day carries ----------

    [Theory]
    [MemberData(nameof(SampleYears))]
    public void EveryDaysSeasonAndFestivityMatchTheCalendar(int year)
    {
        foreach (var day in Cal.Outline(year).OfType<WeekRow>().SelectMany(w => w.Days))
        {
            Assert.Equal(SolsticaCalendar.SeasonOf(day.Date), day.Season);
            Assert.Equal(day.Date.IsFestivity, day.IsFestivity);
        }
    }

    [Theory]
    [MemberData(nameof(SampleYears))]
    public void EveryDaysGregorianDateIsTheOneTheCalendarConverts(int year)
    {
        foreach (var day in Cal.Outline(year).OfType<WeekRow>().SelectMany(w => w.Days))
            Assert.Equal(Cal.CanConvert(day.Date) ? Cal.ToGregorian(day.Date) : null, day.Gregorian);
    }

    [Fact]
    public void TheGregorianDateRunsOutBeforeTheYearDoes()
    {
        // Solstica 10000 begins on 21 December 9999 and outlives DateOnly ten days later.
        var days = Cal.Outline(10000).OfType<WeekRow>().SelectMany(w => w.Days).ToList();

        Assert.Equal(new DateOnly(9999, 12, 31), days[10].Gregorian);
        Assert.Equal(Cal.MaxRepresentable, days[10].Date);
        Assert.All(days.Take(11), d => Assert.NotNull(d.Gregorian));
        Assert.All(days.Skip(11), d => Assert.Null(d.Gregorian));
    }

    // ---------- range ----------

    [Fact]
    public void YearsTheCalendarDoesNotCoverAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Cal.Outline(2026));  // before the epoch
        Assert.Throws<ArgumentOutOfRangeException>(() => Cal.Outline(10001)); // past the table
    }

    // ---------- helpers ----------

    /// <summary>A common and a leap year around every structural landmark of the window.</summary>
    public static TheoryData<int> SampleYears =>
        [2027, 2028, 2095, 2096, 3152, 3323, 3324, 5000, 5508, 8656, 9999, 10000];

    private static IEnumerable<SolsticaDate> DaysOf(IEnumerable<OutlineRow> rows) =>
        rows.SelectMany(r => r switch
        {
            WeekRow w => w.Days.Select(d => d.Date),
            ExtraWeeklyRow e => new[] { e.Day.Date },
            _ => []
        });

    private static int SupertagoAt(IReadOnlyList<OutlineRow> rows)
    {
        for (int i = 0; i < rows.Count; i++)
            if (rows[i] is ExtraWeeklyRow { Day.Date.Period: PeriodKind.Supertago }) return i;
        return -1;
    }

    private static int LeapYearIn(ValidityPeriod period)
    {
        // The first period opens before the epoch, and the outline begins at 1 Unua 2027.
        int year = Math.Max(period.FirstYear, SolsticaEpoch.Expository2026.FirstSolsticaYear);
        while (!SolsticaCalendar.IsLeapYear(year)) year++;
        Assert.True(period.Contains(year), $"no leap year in {period.FirstYear}-{period.LastYear}.");
        return year;
    }
}
