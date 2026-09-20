using Xunit;

namespace SolsticaKalendaro.Core.Tests;

public class SolsticaCalendarTests
{
    private static readonly SolsticaCalendar Cal = new(SolsticaEpoch.Expository2026);

    // ---------- the table of section 9.3 ----------

    [Fact]
    public void EveryPeriodIsWellFormed()
    {
        foreach (var p in ValidityPeriod.Table)
        {
            Assert.True(p.Allocation.IsWellFormed, $"{p.FirstYear}: allocation {p.Allocation} does not total 365.");
            Assert.True(p.Blocks.IsWellFormed, $"{p.FirstYear}: blocks {p.Blocks} are not multiples of 7 totalling 28.");
            Assert.Equal(0, p.SupertagoSeam % 7); // must be a Sunday|Monday seam
        }
    }

    [Fact]
    public void PeriodsTileTheWindowWithoutGapOrOverlap()
    {
        for (int i = 1; i < ValidityPeriod.Table.Count; i++)
            Assert.Equal(ValidityPeriod.Table[i - 1].LastYear + 1, ValidityPeriod.Table[i].FirstYear);
    }

    [Fact]
    public void ArchitectureConstraintHolds()
    {
        // Section 4.6: each of the outer seasons can borrow only from the block beside it,
        // so s1 <= 84 + w1 and s4 <= 85 + w3 (the Jarfino supplies the extra day).
        foreach (var p in ValidityPeriod.Table)
        {
            Assert.InRange(p.Allocation.First, 84, 84 + p.Blocks.EkvinoksoI);
            Assert.True(p.Allocation.Fourth <= 85 + p.Blocks.EkvinoksoII,
                $"{p.FirstYear}: s4 = {p.Allocation.Fourth} exceeds {85 + p.Blocks.EkvinoksoII}.");
        }
    }

    [Theory]
    [InlineData(2027, 3)] // III, mid-Jarmezo
    [InlineData(2100, 4)] // IV, after Ekvinokso II
    [InlineData(3200, 1)] // I, before Ekvinokso I
    [InlineData(4000, 4)]
    [InlineData(5000, 2)] // II, before Jarmezo
    [InlineData(8000, 4)] // IV, mid-Ekvinokso II
    public void SupertagoLandsInTheDeclaredSeason(int year, int season)
    {
        Assert.Equal((Season)season, ValidityPeriod.For(year).SupertagoSeason);
    }

    [Fact]
    public void ExactlyOneStructuralReform()
    {
        var changes = ValidityPeriod.Table.Skip(1)
            .Where((p, i) => p.Blocks != ValidityPeriod.Table[i].Blocks)
            .ToList();
        Assert.Single(changes);
        Assert.Equal(ValidityPeriod.StructuralReformYear, changes[0].FirstYear);
        Assert.Equal(BlockWidths.SevenSevenFourteen, changes[0].Blocks);
    }

    [Fact]
    public void TheReformMovesOnlySepaOkaAndNaua()
    {
        // Section 9.4: days 1..175 do not move, the last three months and the Jarfino
        // return to exactly their former positions, and three months shift seven days earlier.
        var before = YearLayout.For(BlockWidths.SevenFourteenSeven);
        var after = YearLayout.For(BlockWidths.SevenSevenFourteen);

        foreach (var p in new[] { PeriodKind.Unua, PeriodKind.Dua, PeriodKind.Tria,
                                  PeriodKind.EkvinoksoI, PeriodKind.Kvara, PeriodKind.Kvina,
                                  PeriodKind.Sesa, PeriodKind.Jarmezo })
            Assert.Equal(before.Start(p), after.Start(p));

        foreach (var p in new[] { PeriodKind.Sepa, PeriodKind.Oka, PeriodKind.Naua })
            Assert.Equal(before.Start(p) - 7, after.Start(p));

        foreach (var p in new[] { PeriodKind.Deka, PeriodKind.DekUnua, PeriodKind.DekDua, PeriodKind.Jarfino })
            Assert.Equal(before.Start(p), after.Start(p));
    }

    // ---------- structure ----------

    [Theory]
    [InlineData(2027, 365)]
    [InlineData(2028, 366)]
    [InlineData(2100, 365)]
    [InlineData(2400, 366)]
    public void YearHasCorrectLength(int year, int expected)
    {
        Assert.Equal(expected, SolsticaCalendar.DaysInYear(year));
        var layout = ValidityPeriod.For(year).Layout;
        int sum = YearLayout.Sequence.Sum(layout.Length) + (SolsticaCalendar.IsLeapYear(year) ? 1 : 0);
        Assert.Equal(expected, sum);
    }

    [Fact]
    public void EpochMapsToFirstDay()
    {
        Assert.Equal(new DateOnly(2026, 12, 21),
            Cal.ToGregorian(new SolsticaDate(2027, PeriodKind.Unua, 1)));
    }

    [Fact]
    public void NewYearFallsOnTheSameGregorianDateForever()
    {
        // Both calendars use 4/100/400 with the same numbering, so they never drift apart.
        for (int y = 2027; y <= 9999; y++)
            Assert.Equal(new DateOnly(y - 1, 12, 21),
                Cal.ToGregorian(new SolsticaDate(y, PeriodKind.Unua, 1)));
    }

    // ---------- round trip, across every period boundary ----------

    [Fact]
    public void EveryDayRoundTrips()
    {
        foreach (int y in YearsToSweep())
            for (int o = 1; o <= SolsticaCalendar.DaysInYear(y); o++)
            {
                var sk = SolsticaCalendar.FromDayOfYear(y, o);
                Assert.Equal(o, SolsticaCalendar.DayOfYear(sk));
                Assert.Equal(sk, Cal.FromGregorian(Cal.ToGregorian(sk)));
            }
    }

    [Fact]
    public void ConsecutiveGregorianDaysAreConsecutiveSolsticaDays()
    {
        var d = new DateOnly(2026, 12, 21);
        int previous = 0, year = 2027;
        while (d.Year < 2110) // spans the 2096 recalibration
        {
            var sk = Cal.FromGregorian(d);
            int o = SolsticaCalendar.DayOfYear(sk);
            if (sk.Year != year) { Assert.Equal(1, o); year = sk.Year; }
            else Assert.Equal(previous + 1, o);
            previous = o;
            d = d.AddDays(1);
        }
    }

    // ---------- the leap window ----------

    [Fact]
    public void GregorianLeapDayShiftsTheOffsetUntilTheSupertago()
    {
        // The two calendars insert their extra day at different points: Gregorian at
        // 29 February, Solstica at the Supertago. Between the two the offset differs by one.
        var before = Cal.FromGregorian(new DateOnly(2028, 2, 28));
        var after = Cal.FromGregorian(new DateOnly(2028, 3, 1));
        Assert.Equal(SolsticaCalendar.DayOfYear(before) + 2, SolsticaCalendar.DayOfYear(after));
    }

    [Fact]
    public void SupertagoOnlyExistsInLeapYears()
    {
        Assert.Equal(183, SolsticaCalendar.DayOfYear(SolsticaDate.Supertago(2028)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SolsticaCalendar.DayOfYear(SolsticaDate.Supertago(2027)));
    }

    [Fact]
    public void SupertagoSitsAtTheSeamOfItsPeriod()
    {
        foreach (var p in ValidityPeriod.Table)
        {
            int year = FirstLeapYearFrom(p.FirstYear);
            var seamDay = SolsticaCalendar.FromDayOfYear(year, p.SupertagoSeam);
            var nextDay = SolsticaCalendar.FromDayOfYear(year, p.SupertagoOrdinal + 1);

            Assert.Equal(DayOfWeek.Sunday, SolsticaCalendar.WeekDay(seamDay));
            Assert.Null(SolsticaCalendar.WeekDay(SolsticaDate.Supertago(year)));
            Assert.Equal(DayOfWeek.Monday, SolsticaCalendar.WeekDay(nextDay));
        }
    }

    [Fact]
    public void InTheCurrentPeriodTheSupertagoSitsBetweenSevenAndEightJarmezo()
    {
        var seven = new SolsticaDate(2028, PeriodKind.Jarmezo, 7);
        var eight = new SolsticaDate(2028, PeriodKind.Jarmezo, 8);
        Assert.Equal(Cal.ToGregorian(seven).AddDays(1), Cal.ToGregorian(SolsticaDate.Supertago(2028)));
        Assert.Equal(Cal.ToGregorian(SolsticaDate.Supertago(2028)).AddDays(1), Cal.ToGregorian(eight));
    }

    [Fact]
    public void RecalibrationMovesLeapYearDatesOnlyBetweenTheOldAndNewSeams()
    {
        // 2092 and 2096 are leap years either side of the first reallocation. The seam moves
        // from 182 to 280, so only the days in between shift; the rest of the year is unmoved.
        var oldPeriod = ValidityPeriod.For(2092);
        var newPeriod = ValidityPeriod.For(2096);
        Assert.Equal(182, oldPeriod.SupertagoSeam);
        Assert.Equal(280, newPeriod.SupertagoSeam);
        Assert.Equal(oldPeriod.Blocks, newPeriod.Blocks); // semantic only: no date structure changes

        var date = new SolsticaDate(2096, PeriodKind.Sepa, 1);
        Assert.Equal(190, SolsticaCalendar.DayOfYear(date));                            // not yet shifted
        Assert.Equal(191, SolsticaCalendar.DayOfYear(new SolsticaDate(2092, PeriodKind.Sepa, 1))); // shifted

        var late = new SolsticaDate(2096, PeriodKind.DekDua, 28);
        Assert.Equal(365, SolsticaCalendar.DayOfYear(late));
        Assert.Equal(365, SolsticaCalendar.DayOfYear(new SolsticaDate(2092, PeriodKind.DekDua, 28)));
    }

    [Fact]
    public void CommonYearsAreUnaffectedBySemanticRecalibration()
    {
        // Section 9.4: changing the allocation moves no date. 2095 and 2097 are common years
        // in different periods with the same block arrangement, so every date must coincide.
        foreach (var block in YearLayout.Sequence)
            for (int d = 1; d <= ValidityPeriod.For(2095).Layout.Length(block); d++)
                Assert.Equal(
                    SolsticaCalendar.DayOfYear(new SolsticaDate(2095, block, d)),
                    SolsticaCalendar.DayOfYear(new SolsticaDate(2097, block, d)));
    }

    [Fact]
    public void JarfinoIsAlwaysTheLastDay()
    {
        foreach (int y in new[] { 2027, 2028, 2100, 3324, 8000 })
            Assert.Equal(SolsticaCalendar.DaysInYear(y),
                SolsticaCalendar.DayOfYear(SolsticaDate.Jarfino(y)));
    }

    // ---------- perpetual grid ----------

    [Fact]
    public void EveryBlockStartsOnMondayInEveryPeriod()
    {
        foreach (int y in YearsToSweep())
            foreach (var block in YearLayout.Sequence.Where(b => b != PeriodKind.Jarfino))
            {
                Assert.Equal(DayOfWeek.Monday, SolsticaCalendar.WeekDay(new SolsticaDate(y, block, 1)));
                if (block.IsMonth())
                    foreach (int d in new[] { 8, 15, 22 })
                        Assert.Equal(DayOfWeek.Monday, SolsticaCalendar.WeekDay(new SolsticaDate(y, block, d)));
            }
    }

    [Fact]
    public void ExtraWeeklyDaysHaveNoWeekday()
    {
        Assert.Null(SolsticaCalendar.WeekDay(SolsticaDate.Jarfino(2027)));
        Assert.Null(SolsticaCalendar.WeekDay(SolsticaDate.Supertago(2028)));
        Assert.Equal(DayOfWeek.Sunday, SolsticaCalendar.WeekDay(new SolsticaDate(2027, PeriodKind.DekDua, 28)));
    }

    // ---------- seasons ----------

    [Fact]
    public void SeasonLengthsMatchTheAllocationOfTheirPeriod()
    {
        foreach (var p in ValidityPeriod.Table)
            foreach (int y in new[] { FirstCommonYearFrom(p.FirstYear), FirstLeapYearFrom(p.FirstYear) })
            {
                var counts = Enumerable.Range(1, SolsticaCalendar.DaysInYear(y))
                    .Select(o => SolsticaCalendar.SeasonOf(SolsticaCalendar.FromDayOfYear(y, o)))
                    .GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

                foreach (var s in Enum.GetValues<Season>())
                    Assert.Equal(p.SeasonLength(s, y), counts[s]);
            }
    }

    [Fact]
    public void SupertagoIsAlwaysAbsorbedByATransitionBlock()
    {
        // Section 9.5: the rule never lets a month run to 29 days.
        foreach (var p in ValidityPeriod.Table)
        {
            var (blockBefore, _) = p.Layout.FromCommonOrdinal(p.SupertagoSeam);
            var (blockAfter, _) = p.Layout.FromCommonOrdinal(Math.Min(p.SupertagoSeam + 1, 365));
            Assert.True(blockBefore.IsTransitionBlock() || blockAfter.IsTransitionBlock(),
                $"{p.FirstYear}: Supertago seam {p.SupertagoSeam} touches no transition block.");
        }
    }

    // ---------- epoch ----------

    [Fact]
    public void AnyEpochProducesTheSameCalendar()
    {
        var a = new SolsticaCalendar(SolsticaEpoch.Expository2026);
        var b = new SolsticaCalendar(SolsticaEpoch.AdoptionWindows[1]); // 21 Dec 2037
        var date = new SolsticaDate(2050, PeriodKind.Kvara, 12);
        Assert.Equal(a.ToGregorian(date), b.ToGregorian(date));
    }

    [Fact]
    public void The2031WindowAnchorsOnTwentySecondDecember()
    {
        var e = SolsticaEpoch.AdoptionWindows[0];
        Assert.True(e.IsOffAnchor);
        Assert.Equal(new DateOnly(2031, 12, 22), e.AdoptionDate);
        Assert.Equal(DayOfWeek.Monday, e.AdoptionDate.DayOfWeek);
    }

    [Fact]
    public void DatesBeforeTheEpochAreRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Cal.FromGregorian(new DateOnly(2026, 12, 20)));

    [Fact]
    public void YearsBeyondTheTabulatedWindowAreRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ValidityPeriod.For(10001));

    // ---------- naming ----------

    [Fact]
    public void CanonicalStringForm()
    {
        Assert.Equal("12 Kvara 2027", new SolsticaDate(2027, PeriodKind.Kvara, 12).ToString());
        Assert.Equal("Jarfino 2027", SolsticaDate.Jarfino(2027).ToString());
        Assert.Equal("Jarkomenco", new SolsticaDate(2027, PeriodKind.Unua, 1).FestivityName);
        Assert.Equal("Rekomenco", new SolsticaDate(2027, PeriodKind.Jarmezo, 8).FestivityName);
    }

    [Fact]
    public void RekomencoIsAlwaysOrdinal183()
    {
        // The first transition block is seven days wide in every period, so the Jarmezo always
        // opens on day 176 and always contains the 90-degree solstice. The next Monday is
        // therefore always the opening of the following block.
        foreach (var p in ValidityPeriod.Table)
        {
            Assert.InRange(p.NinetyDegreeSeam, 176, 182);              // solstice inside the Jarmezo
            Assert.Equal(183, p.RekomencoCommonOrdinal);
        }
    }

    [Fact]
    public void RekomencoChangesNameOnlyAtTheStructuralReform()
    {
        Assert.Equal((PeriodKind.Jarmezo, 8), ValidityPeriod.For(2027).Rekomenco);
        Assert.Equal((PeriodKind.Jarmezo, 8), ValidityPeriod.For(3323).Rekomenco);
        Assert.Equal((PeriodKind.Sepa, 1), ValidityPeriod.For(3324).Rekomenco);
        Assert.Equal((PeriodKind.Sepa, 1), ValidityPeriod.For(10000).Rekomenco);
    }

    [Fact]
    public void RekomencoIsAlwaysAMondayAndNeverExtraWeekly()
    {
        foreach (int y in YearsToSweep())
        {
            var (block, day) = ValidityPeriod.For(y).Rekomenco;
            var date = new SolsticaDate(y, block, day);
            Assert.Equal(DayOfWeek.Monday, SolsticaCalendar.WeekDay(date));
            Assert.Equal("Rekomenco", date.FestivityName);
        }
    }

    // ---------- helpers ----------

    /// <summary>One common and one leap year at the start, middle and end of every period.</summary>
    private static IEnumerable<int> YearsToSweep() =>
        ValidityPeriod.Table
            .SelectMany(p => new[] { p.FirstYear, (p.FirstYear + p.LastYear) / 2, p.LastYear })
            .SelectMany(y => new[] { FirstCommonYearFrom(y), FirstLeapYearFrom(y) })
            .Where(y => y >= 2027 && y <= 10000)
            .Distinct();

    private static int FirstLeapYearFrom(int year)
    {
        while (!SolsticaCalendar.IsLeapYear(year)) year++;
        return year;
    }

    private static int FirstCommonYearFrom(int year)
    {
        while (SolsticaCalendar.IsLeapYear(year)) year++;
        return year;
    }
}
