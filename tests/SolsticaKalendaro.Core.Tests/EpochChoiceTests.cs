using Xunit;

namespace SolsticaKalendaro.Core.Tests;

/// <summary>
/// What changing the epoch does, and — mostly — what it does not. An interface that lets the
/// reader choose one has to know which of those two it is dealing with.
/// </summary>
public class EpochChoiceTests
{
    private static readonly SolsticaCalendar Expository = new(SolsticaEpoch.Expository2026);

    [Theory]
    [MemberData(nameof(EpochsAnchoredOn21December))]
    public void AnEpochAnchoredOnTheSameDayAgreesWithTheExpositoryOneEverywhereTheyOverlap(int firstYear)
    {
        // Every option but one anchors 1 Unua on 21 December, and both calendars use 4/100/400
        // with the same year numbering, so where they overlap they are the same calendar.
        // Choosing between them removes or adds years at the start; it moves no date.
        var epoch = Epoch(firstYear);
        var calendar = new SolsticaCalendar(epoch);

        var sample = GregorianSample(epoch.AdoptionDate).ToList();
        Assert.True(sample.Count >= 20, $"only {sample.Count} days sampled; the test proves little.");

        foreach (var day in sample)
        {
            Assert.Equal(Expository.FromGregorian(day), calendar.FromGregorian(day));
            Assert.Equal(day, calendar.ToGregorian(calendar.FromGregorian(day)));
        }
    }

    [Fact]
    public void The2031WindowMovesEveryDateOneDayLater()
    {
        // It anchors on 22 December rather than 21, and that one day is carried for ever.
        var offAnchor = new SolsticaCalendar(SolsticaEpoch.AdoptionWindows[0]);
        Assert.True(SolsticaEpoch.AdoptionWindows[0].IsOffAnchor);

        var sample = SolsticaSample(from: 2032).ToList();
        Assert.True(sample.Count >= 20, $"only {sample.Count} days sampled; the test proves little.");

        foreach (var date in sample)
            Assert.Equal(Expository.ToGregorian(date).AddDays(1), offAnchor.ToGregorian(date));
    }

    [Fact]
    public void The2031WindowLosesTheLastRepresentableDay()
    {
        // Everything is a day later, so the last day .NET can express arrives a day earlier in
        // the year: 10 Unua 10000 rather than 11.
        var offAnchor = new SolsticaCalendar(SolsticaEpoch.AdoptionWindows[0]);

        Assert.Equal(new SolsticaDate(10000, PeriodKind.Unua, 10), offAnchor.MaxRepresentable);
        Assert.Equal(new SolsticaDate(10000, PeriodKind.Unua, 11), Expository.MaxRepresentable);
    }

    // ---------- the samples ----------

    /// <summary>Every option the app offers whose 1 Unua falls on 21 December.</summary>
    public static TheoryData<int> EpochsAnchoredOn21December =>
        [.. new[] { SolsticaEpoch.Expository2026 }
            .Concat(SolsticaEpoch.AdoptionWindows)
            .Where(e => !e.IsOffAnchor)
            .Select(e => e.FirstSolsticaYear)];

    private static SolsticaEpoch Epoch(int firstYear) =>
        new[] { SolsticaEpoch.Expository2026 }.Concat(SolsticaEpoch.AdoptionWindows)
            .Single(e => e.FirstSolsticaYear == firstYear);

    /// <summary>
    /// Gregorian days from where this epoch begins to the end of what DateOnly can hold: the
    /// first day itself, the day every other option begins on, leap days, the days around a
    /// Jarfino and a Supertago, and the very last one.
    /// </summary>
    private static IEnumerable<DateOnly> GregorianSample(DateOnly from)
    {
        var days = new List<DateOnly> { from, DateOnly.MaxValue };

        // Where each of the other options would have started.
        days.AddRange(new[] { SolsticaEpoch.Expository2026 }.Concat(SolsticaEpoch.AdoptionWindows)
            .Select(e => e.AdoptionDate));

        // Leap days, and the turn of a century that is not one.
        days.AddRange([new DateOnly(2028, 2, 29), new DateOnly(2100, 2, 28), new DateOnly(2400, 2, 29),
                       new DateOnly(9996, 2, 29)]);

        // Around the extra-weekly days, where the two calendars are furthest from agreeing.
        var reference = new SolsticaCalendar(SolsticaEpoch.Expository2026);
        foreach (int year in new[] { 2028, 2029, 3324, 7722 })
        {
            // A Supertago only exists in a leap year: asking for one elsewhere names no day.
            var extraWeekly = SolsticaCalendar.IsLeapYear(year)
                ? new[] { SolsticaDate.Jarfino(year), SolsticaDate.Supertago(year) }
                : [SolsticaDate.Jarfino(year)];

            foreach (var date in extraWeekly)
            {
                var gregorian = reference.ToGregorian(date);
                days.AddRange([gregorian.AddDays(-1), gregorian, gregorian.AddDays(1)]);
            }
        }

        // And a spread across the whole window, so the sample is not only its landmarks.
        for (int year = from.Year; year < 9999; year += 613)
            days.Add(new DateOnly(year, 6, 15));

        return days.Where(d => d >= from).Distinct().Order();
    }

    /// <summary>Solstica days of every shape, from the year given onwards.</summary>
    private static IEnumerable<SolsticaDate> SolsticaSample(int from)
    {
        foreach (int year in new[] { from, from + 1, 2096, 3324, 4503, 7722, 9843, 9999 })
        {
            if (year < from || !ValidityPeriod.IsTabulated(year)) continue;

            yield return new SolsticaDate(year, PeriodKind.Unua, 1);
            yield return new SolsticaDate(year, PeriodKind.Jarmezo, 1);
            yield return new SolsticaDate(year, PeriodKind.DekDua, 28);
            yield return SolsticaDate.Jarfino(year);

            if (SolsticaCalendar.IsLeapYear(year)) yield return SolsticaDate.Supertago(year);
        }
    }
}
