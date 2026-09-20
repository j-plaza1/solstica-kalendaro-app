namespace SolsticaKalendaro.Core;

/// <summary>
/// Bidirectional conversion between the Gregorian calendar and the Solstica Kalendaro.
///
/// All arithmetic runs over <see cref="DateOnly.DayNumber"/>, an exact integer day count, so no
/// floating-point Julian Day is involved and no rounding error is possible.
///
/// Conversion is period-aware (section 9.3), but the dependency is narrower than it looks.
/// In a <b>common year</b> only the block arrangement matters, and that changes once in eight
/// thousand years. In a <b>leap year</b> the Supertago seam matters too, and it moves whenever
/// the deficient season changes: the named days lying between the old seam and the new one fall
/// one Gregorian day earlier or later. Nothing else in the calendar notices.
/// </summary>
public sealed class SolsticaCalendar(SolsticaEpoch epoch)
{
    public SolsticaEpoch Epoch { get; } = epoch;

    public static bool IsLeapYear(int year) =>
        year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);

    public static int DaysInYear(int year) => IsLeapYear(year) ? 366 : 365;

    public static ValidityPeriod PeriodFor(int year) => ValidityPeriod.For(year);

    // ---------- ordinal <-> SolsticaDate ----------

    /// <summary>1-based day of the year, counting extra-weekly days.</summary>
    public static int DayOfYear(SolsticaDate date)
    {
        var period = ValidityPeriod.For(date.Year);
        bool leap = IsLeapYear(date.Year);

        if (date.Period == PeriodKind.Supertago)
        {
            if (!leap) throw new ArgumentOutOfRangeException(nameof(date), $"No Supertago in {date.Year}.");
            return period.SupertagoOrdinal;
        }

        int ordinal = period.Layout.CommonOrdinal(date.Period, date.Day);
        return leap && ordinal > period.SupertagoSeam ? ordinal + 1 : ordinal;
    }

    public static SolsticaDate FromDayOfYear(int year, int ordinal)
    {
        if (ordinal < 1 || ordinal > DaysInYear(year))
            throw new ArgumentOutOfRangeException(nameof(ordinal));

        var period = ValidityPeriod.For(year);
        if (IsLeapYear(year))
        {
            if (ordinal == period.SupertagoOrdinal) return SolsticaDate.Supertago(year);
            if (ordinal > period.SupertagoOrdinal) ordinal--;
        }

        var (block, day) = period.Layout.FromCommonOrdinal(ordinal);
        return new SolsticaDate(year, block, day);
    }

    // ---------- Gregorian <-> Solstica ----------

    public DateOnly ToGregorian(SolsticaDate date)
    {
        EnsureInRange(date.Year);
        return DateOnly.FromDayNumber(Epoch.YearStart(date.Year).DayNumber + DayOfYear(date) - 1);
    }

    public SolsticaDate FromGregorian(DateOnly date)
    {
        var anchorThisYear = new DateOnly(date.Year, Epoch.AnchorMonth, Epoch.AnchorDay);
        int solsticaYear = date >= anchorThisYear ? date.Year + 1 : date.Year;
        EnsureInRange(solsticaYear);
        int ordinal = date.DayNumber - Epoch.YearStart(solsticaYear).DayNumber + 1;
        return FromDayOfYear(solsticaYear, ordinal);
    }

    public SolsticaDate FromGregorian(DateTime date) => FromGregorian(DateOnly.FromDateTime(date));

    public SolsticaDate Today() => FromGregorian(DateOnly.FromDateTime(DateTime.Now));

    public bool IsInRange(int solsticaYear) =>
        solsticaYear >= Epoch.FirstSolsticaYear && ValidityPeriod.IsTabulated(solsticaYear);

    private void EnsureInRange(int solsticaYear)
    {
        if (solsticaYear < Epoch.FirstSolsticaYear)
            throw new ArgumentOutOfRangeException(nameof(solsticaYear),
                $"Calendar begins at 1 Unua {Epoch.FirstSolsticaYear}.");
        if (!ValidityPeriod.IsTabulated(solsticaYear))
            throw new ArgumentOutOfRangeException(nameof(solsticaYear),
                $"Year {solsticaYear} lies outside the tabulated window.");
    }

    // ---------- week ----------

    /// <summary>
    /// Weekday within the Solstica week. Null for extra-weekly days, which pause the seven-day
    /// cycle rather than belonging to it. 1 Unua is always Monday, and so is day 1, 8, 15 and 22
    /// of every month and day 1 of every transition block, in every period: each block is a
    /// multiple of seven, so every block seam is a Sunday|Monday seam (section 8.3).
    ///
    /// This diverges from the Gregorian weekday of the same physical day after the first Jarfino.
    /// That divergence is the design's central trade-off, not a defect.
    /// </summary>
    public static DayOfWeek? WeekDay(SolsticaDate date)
    {
        if (date.Period.IsExtraWeekly()) return null;
        var period = ValidityPeriod.For(date.Year);
        int ordinal = DayOfYear(date);
        int weekly = ordinal - 1;
        if (IsLeapYear(date.Year) && ordinal > period.SupertagoOrdinal) weekly--; // Supertago precedes
        return (DayOfWeek)((weekly % 7 + 1) % 7);
    }

    // ---------- seasons ----------

    public static Season SeasonOf(SolsticaDate date)
    {
        var period = ValidityPeriod.For(date.Year);
        if (date.Period == PeriodKind.Supertago) return period.SupertagoSeason;

        int ordinal = period.Layout.CommonOrdinal(date.Period, date.Day);
        return period.Allocation.SeasonOf(ordinal);
    }

    /// <summary>Position within the year in [0, 1). Drives the seasonal position bar.</summary>
    public static double YearFraction(SolsticaDate date) =>
        (DayOfYear(date) - 1) / (double)DaysInYear(date.Year);
}
