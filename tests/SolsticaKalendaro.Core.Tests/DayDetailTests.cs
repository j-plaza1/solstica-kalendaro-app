using Xunit;

namespace SolsticaKalendaro.Core.Tests;

public class DayDetailTests
{
    private static readonly SolsticaCalendar Cal = new(SolsticaEpoch.Expository2026);

    // ---------- where the day sits ----------

    [Fact]
    public void ADayKnowsItsPlaceInTheYearAndInItsSeason()
    {
        var detail = Cal.Describe(new SolsticaDate(2028, PeriodKind.Kvara, 12));

        Assert.Equal(new DateOnly(2028, 4, 1), detail.Gregorian);
        Assert.Equal(103, detail.DayOfYear);
        Assert.Equal(366, detail.DaysInYear);
        Assert.Equal(Season.Second, detail.Season);
        Assert.Equal(14, detail.DayOfSeason);
        Assert.Equal(93, detail.SeasonLength);
        Assert.Equal(ValidityPeriod.Current, detail.Period);
    }

    [Theory]
    [MemberData(nameof(SampleYears))]
    public void TheSeasonsTileTheYearWithoutGapOrOverlap(int year)
    {
        // Walking the year, the day of the season restarts at 1 exactly when the season turns,
        // and reaches its length exactly as it ends. The Supertago is counted in its own season.
        var seen = new Dictionary<Season, int>();

        for (int ordinal = 1; ordinal <= SolsticaCalendar.DaysInYear(year); ordinal++)
        {
            var detail = Cal.Describe(SolsticaCalendar.FromDayOfYear(year, ordinal));
            int expected = seen.GetValueOrDefault(detail.Season) + 1;

            Assert.Equal(expected, detail.DayOfSeason);
            Assert.InRange(detail.DayOfSeason, 1, detail.SeasonLength);
            seen[detail.Season] = expected;
        }

        foreach (var (season, counted) in seen)
            Assert.Equal(ValidityPeriod.For(year).SeasonLength(season, year), counted);
    }

    // ---------- the two weekdays, and what moves them apart ----------

    [Fact]
    public void BeforeTheFirstJarfinoTheTwoCalendarsAgreeOnTheWeekday()
    {
        for (int ordinal = 1; ordinal < SolsticaCalendar.DaysInYear(2027); ordinal++)
        {
            var detail = Cal.Describe(SolsticaCalendar.FromDayOfYear(2027, ordinal));

            Assert.Equal(detail.GregorianWeekDay, detail.WeekDay);
            Assert.Null(detail.LastShift);
            Assert.Equal(SolsticaDate.Jarfino(2027), detail.NextShift);
        }
    }

    [Fact]
    public void AfterTheFirstJarfinoTheWeekdaysDifferByOneDay()
    {
        var detail = Cal.Describe(new SolsticaDate(2028, PeriodKind.Kvara, 12));

        Assert.Equal(DayOfWeek.Friday, detail.WeekDay);
        Assert.Equal(DayOfWeek.Saturday, detail.GregorianWeekDay);
        Assert.Equal(SolsticaDate.Jarfino(2027), detail.LastShift);
        Assert.Equal(SolsticaDate.Supertago(2028), detail.NextShift);
    }

    [Theory]
    [MemberData(nameof(SampleYears))]
    public void TheGapBetweenTheWeekdaysIsTheCountOfExtraWeeklyDaysSoFar(int year)
    {
        // This is the whole of the divergence: every extra-weekly day pauses the seven-day cycle
        // once, so the Solstica weekday falls one further behind the Gregorian one.
        for (int ordinal = 1; ordinal <= SolsticaCalendar.DaysInYear(year); ordinal++)
        {
            var detail = Cal.Describe(SolsticaCalendar.FromDayOfYear(year, ordinal));
            if (detail.WeekDay is not { } solstica) continue;    // an extra-weekly day has none

            int gap = ((int)detail.GregorianWeekDay! - (int)solstica + 7) % 7;
            Assert.Equal(ExtraWeeklyDaysBefore(detail.Date) % 7, gap);
        }
    }

    [Fact]
    public void TheExtraWeeklyDaysOnEitherSideAreTheNearestOnes()
    {
        // Sweeping a leap year, the pair walks with the date: last one behind, next one ahead,
        // and each of the year's own extra-weekly days takes its turn in both.
        SolsticaDate? previous = SolsticaDate.Jarfino(2027);

        for (int ordinal = 1; ordinal <= SolsticaCalendar.DaysInYear(2028); ordinal++)
        {
            var date = SolsticaCalendar.FromDayOfYear(2028, ordinal);
            var detail = Cal.Describe(date);

            Assert.Equal(previous, detail.LastShift);
            Assert.NotNull(detail.NextShift);
            Assert.True(SolsticaCalendar.DayOfYear(detail.NextShift!.Value) > ordinal
                        || detail.NextShift!.Value.Year > 2028);

            if (date.Period.IsExtraWeekly()) previous = date;
        }

        Assert.Equal(SolsticaDate.Jarfino(2028), previous);
    }

    [Fact]
    public void TheFirstDayOfAllHasNothingBehindIt()
    {
        var detail = Cal.Describe(new SolsticaDate(2027, PeriodKind.Unua, 1));

        Assert.Null(detail.LastShift);
        Assert.Equal(detail.GregorianWeekDay, detail.WeekDay);
        Assert.Equal(DayOfWeek.Monday, detail.WeekDay);
    }

    [Fact]
    public void AnExtraWeeklyDayHasNoWeekdayButStillHasAGregorianOne()
    {
        var detail = Cal.Describe(SolsticaDate.Jarfino(2027));

        Assert.Null(detail.WeekDay);
        Assert.NotNull(detail.GregorianWeekDay);
        Assert.Null(detail.LastShift);                          // itself does not count
        Assert.Equal(SolsticaDate.Supertago(2028), detail.NextShift);
    }

    // ---------- festivities ----------

    [Fact]
    public void TheNextFestivitiesAreTheOnesTheCanvasShows()
    {
        var upcoming = Cal.UpcomingFestivities(new SolsticaDate(2028, PeriodKind.Kvara, 12), 3);

        Assert.Equal(["Supertago", "Rekomenco", "Jarfino"], upcoming.Select(f => f.Name));
        Assert.Equal([80, 81, 263], upcoming.Select(f => f.DaysAway));
        Assert.Equal(new DateOnly(2028, 6, 20), upcoming[0].Gregorian);
    }

    [Fact]
    public void TheListCrossesTheTurnOfTheYear()
    {
        var upcoming = Cal.UpcomingFestivities(SolsticaDate.Jarfino(2027), 2);

        Assert.Equal([2028, 2028], upcoming.Select(f => f.Date.Year));
        Assert.Equal("Jarkomenco", upcoming[0].Name);
        Assert.Equal(1, upcoming[0].DaysAway);                  // the very next day
    }

    [Theory]
    [MemberData(nameof(SampleYears))]
    public void FestivitiesComeInOrderAndOnlyFromAhead(int year)
    {
        var from = SolsticaCalendar.FromDayOfYear(year, 1);
        var upcoming = Cal.UpcomingFestivities(from, 12);

        Assert.All(upcoming, f => Assert.True(f.DaysAway > 0, $"{f.Date} is not ahead of {from}."));
        Assert.Equal(upcoming.Select(f => f.DaysAway).Order(), upcoming.Select(f => f.DaysAway));
        Assert.All(upcoming, f => Assert.True(f.Date.IsFestivity, $"{f.Date} is not a festivity."));
        Assert.All(upcoming, f => Assert.Equal(f.Gregorian, Cal.ToGregorian(f.Date)));
    }

    [Fact]
    public void TheListStopsAtTheEndOfWhatCanBeConvertedInsteadOfThrowing()
    {
        // Solstica 10000 runs past DateOnly ten days in, so the festivities of that year have no
        // Gregorian date to give. Asking from just before it returns what it can and stops.
        var upcoming = Cal.UpcomingFestivities(new SolsticaDate(9999, PeriodKind.DekDua, 28), 5);

        Assert.All(upcoming, f => Assert.NotNull(f.Gregorian));
        Assert.All(upcoming, f => Assert.True(Cal.CanConvert(f.Date)));
        Assert.True(upcoming.Count < 5, "expected the list to stop short of what was asked.");
    }

    [Fact]
    public void AskingForNothingReturnsNothing() =>
        Assert.Empty(Cal.UpcomingFestivities(new SolsticaDate(2028, PeriodKind.Unua, 1), 0));

    // ---------- range ----------

    [Fact]
    public void DaysTheCalendarDoesNotCoverAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Cal.Describe(new SolsticaDate(2026, PeriodKind.Unua, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cal.Describe(new SolsticaDate(10001, PeriodKind.Unua, 1)));
    }

    // ---------- helpers ----------

    public static TheoryData<int> SampleYears => [2027, 2028, 2095, 2096, 3323, 3324, 5508];

    /// <summary>Extra-weekly days from the epoch up to, but not counting, this date.</summary>
    private static int ExtraWeeklyDaysBefore(SolsticaDate date)
    {
        int count = 0;
        for (int year = SolsticaEpoch.Expository2026.FirstSolsticaYear; year <= date.Year; year++)
        {
            if (SolsticaCalendar.IsLeapYear(year)
                && (year < date.Year
                    || ValidityPeriod.For(year).SupertagoOrdinal < SolsticaCalendar.DayOfYear(date)))
                count++;

            if (year < date.Year) count++;                      // that year's Jarfino
        }
        return count;
    }
}
