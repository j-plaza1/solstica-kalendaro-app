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
    /// <summary>
    /// The version of the proposal document this code implements, as major.minor. It is the
    /// first half of a release tag: an app version says what it is against a given version of
    /// the document, and the release workflow refuses a tag that disagrees with this.
    ///
    /// Raise it when the document is revised and this code is brought in line with it, in the
    /// same change. It is not a version of the library.
    /// </summary>
    public const string SpecificationVersion = "2.1";

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
        long dayNumber = (long)Epoch.YearStart(date.Year).DayNumber + DayOfYear(date) - 1;
        if (dayNumber > DateOnly.MaxValue.DayNumber)
            throw new ArgumentOutOfRangeException(nameof(date),
                $"{date} falls after {DateOnly.MaxValue:yyyy-MM-dd}, the last date .NET's Gregorian "
                + $"calendar represents. The last convertible date is {MaxRepresentable}.");
        return DateOnly.FromDayNumber((int)dayNumber);
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

    /// <summary>
    /// The last Solstica date this calendar can convert. Solstica year 10000 begins on
    /// 21 December 9999 and runs into Gregorian year 10000, which DateOnly cannot
    /// represent, so its last 355 days have no Gregorian counterpart in .NET. The
    /// boundary moves with the epoch's anchor, which is why this is an instance member.
    /// </summary>
    public SolsticaDate MaxRepresentable
    {
        get
        {
            int last = ValidityPeriod.Table[^1].LastYear;
            int ordinal = DateOnly.MaxValue.DayNumber - Epoch.YearStart(last).DayNumber + 1;
            return ordinal >= DaysInYear(last)
                ? SolsticaDate.Jarfino(last)
                : FromDayOfYear(last, ordinal);
        }
    }

    public bool CanConvert(SolsticaDate date) =>
        IsInRange(date.Year)
        && (long)Epoch.YearStart(date.Year).DayNumber + DayOfYear(date) - 1 <= DateOnly.MaxValue.DayNumber;

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

    // ---------- one day in full ----------

    /// <summary>
    /// Everything true of a single day: where it sits in the year and in its season, what both
    /// calendars call its weekday, and the extra-weekly days on either side of it that explain
    /// why those two weekdays differ.
    /// </summary>
    public DayDetail Describe(SolsticaDate date)
    {
        EnsureInRange(date.Year);
        var period = ValidityPeriod.For(date.Year);
        var season = SeasonOf(date);
        var gregorian = CanConvert(date) ? ToGregorian(date) : (DateOnly?)null;

        return new DayDetail(
            date,
            gregorian,
            WeekDay(date),
            gregorian?.DayOfWeek,
            DayOfYear(date),
            DaysInYear(date.Year),
            season,
            DayOfYear(date) - SeasonStart(period, season, date.Year) + 1,
            period.SeasonLength(season, date.Year),
            period,
            ShiftBefore(date),
            ShiftAfter(date));
    }

    /// <summary>
    /// Day of the year the season opens on. The Supertago can be that day: it is assigned to the
    /// deficient season, and where that season begins at the seam, the intercalated day arrives
    /// before the season's first ordinary day rather than after it.
    /// </summary>
    private static int SeasonStart(ValidityPeriod period, Season season, int year)
    {
        int common = 1;
        foreach (var s in Enum.GetValues<Season>())
        {
            if (s == season) break;
            common += period.Allocation[s];
        }

        if (!IsLeapYear(year)) return common;

        int start = common > period.SupertagoSeam ? common + 1 : common;
        return period.SupertagoSeason == season
            ? Math.Min(start, period.SupertagoOrdinal)
            : start;
    }

    // ---------- the extra-weekly days, which are what moves the two weekdays apart ----------

    /// <summary>
    /// The extra-weekly days of a year, in the order they fall. Every year has a Jarfino; a leap
    /// year has a Supertago before it, at the seam its period puts it on.
    /// </summary>
    private static IEnumerable<SolsticaDate> ExtraWeeklyDaysOf(int year)
    {
        if (IsLeapYear(year)) yield return SolsticaDate.Supertago(year);
        yield return SolsticaDate.Jarfino(year);
    }

    private SolsticaDate? ShiftBefore(SolsticaDate date)
    {
        int ordinal = DayOfYear(date);

        for (int year = date.Year; year >= Epoch.FirstSolsticaYear; year--)
            foreach (var day in ExtraWeeklyDaysOf(year).Reverse())
                if (year < date.Year || DayOfYear(day) < ordinal)
                    return day;

        return null;
    }

    private SolsticaDate? ShiftAfter(SolsticaDate date)
    {
        int ordinal = DayOfYear(date);

        for (int year = date.Year; ValidityPeriod.IsTabulated(year); year++)
            foreach (var day in ExtraWeeklyDaysOf(year))
                if (year > date.Year || DayOfYear(day) > ordinal)
                    return day;

        return null;
    }

    // ---------- festivities ----------

    /// <summary>
    /// The universal holidays of section 8.3 that follow <paramref name="from"/>, in order,
    /// crossing the turn of the year as needed. It stops early rather than throwing: at the end
    /// of the tabulated window, and at <see cref="MaxRepresentable"/>, past which a holiday has
    /// no Gregorian date to be given.
    /// </summary>
    public IReadOnlyList<Festivity> UpcomingFestivities(SolsticaDate from, int count)
    {
        EnsureInRange(from.Year);
        if (count <= 0) return [];

        var found = new List<Festivity>(count);
        int ordinal = DayOfYear(from);

        for (int year = from.Year; ValidityPeriod.IsTabulated(year) && found.Count < count; year++)
            foreach (var date in FestivitiesOf(year))
            {
                if (year == from.Year && DayOfYear(date) <= ordinal) continue;
                if (!CanConvert(date)) return found;

                found.Add(new Festivity(date, date.FestivityName!, ToGregorian(date), DaysBetween(from, date)));
                if (found.Count == count) break;
            }

        return found;
    }

    /// <summary>A year's universal holidays, in the order they fall.</summary>
    private static IEnumerable<SolsticaDate> FestivitiesOf(int year)
    {
        var period = ValidityPeriod.For(year);
        var (block, day) = period.Rekomenco;

        return new[]
        {
            new SolsticaDate(year, PeriodKind.Unua, 1),          // Jarkomenco
            new SolsticaDate(year, block, day),                  // Rekomenco
            SolsticaDate.Supertago(year),
            SolsticaDate.Jarfino(year)
        }
        .Where(d => d.Period != PeriodKind.Supertago || IsLeapYear(year))
        .OrderBy(DayOfYear);
    }

    /// <summary>
    /// Physical days from one date to a later one. Counted over whole years rather than through
    /// the Gregorian calendar, so it still answers past the end of what DateOnly can represent.
    /// </summary>
    private static int DaysBetween(SolsticaDate from, SolsticaDate to)
    {
        int days = DayOfYear(to) - DayOfYear(from);
        for (int year = from.Year; year < to.Year; year++) days += DaysInYear(year);
        return days;
    }

    // ---------- outline ----------

    /// <summary>
    /// The whole year as the rows an interface paints from top to bottom: a header opening
    /// each month and each transition block, the weeks of that block, and the extra-weekly
    /// days at the ordinal where they fall.
    ///
    /// Rows are in ordinal order. The Supertago sits where its validity period puts it
    /// rather than always inside the Jarmezo: between the Tria and the Ekvinokso I in
    /// 3151-3323, for instance (section 9.3). Like the Jarfino, which closes the year, it
    /// opens no section: both stand outside the seven-day cycle, and the block they
    /// interrupt resumes after them.
    ///
    /// This is an instance member because a day's Gregorian date depends on the epoch.
    /// </summary>
    public IReadOnlyList<OutlineRow> Outline(int year)
    {
        EnsureInRange(year);

        var rows = new List<OutlineRow>();
        var week = new List<OutlineDay>(7);
        PeriodKind? section = null;

        for (int ordinal = 1; ordinal <= DaysInYear(year); ordinal++)
        {
            var date = FromDayOfYear(year, ordinal);

            if (date.Period.IsExtraWeekly())
            {
                rows.Add(new ExtraWeeklyRow(AsOutlineDay(date)));
                continue;
            }

            if (section != date.Period)
            {
                section = date.Period;
                rows.Add(new SectionHeader(date.Period));
            }

            week.Add(AsOutlineDay(date));

            // Every block is a whole number of weeks, so this never closes across a seam.
            if (week.Count == 7)
            {
                rows.Add(new WeekRow(date.Period, [.. week]));
                week.Clear();
            }
        }

        return rows;
    }

    private OutlineDay AsOutlineDay(SolsticaDate date) => new(
        date,
        CanConvert(date) ? ToGregorian(date) : null,
        SeasonOf(date),
        date.IsFestivity);
}
