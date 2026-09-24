namespace SolsticaKalendaro.Core;

/// <summary>
/// Everything a single day answers to, gathered in one place. Data only: the core states no
/// sentence about a day, it only says what is true of it.
/// </summary>
/// <param name="Gregorian">Null past what <see cref="DateOnly"/> can represent.</param>
/// <param name="WeekDay">The Solstica weekday. Null for the extra-weekly days, which have none.</param>
/// <param name="GregorianWeekDay">
/// The weekday of the same physical day in the Gregorian calendar. It agrees with
/// <paramref name="WeekDay"/> until the first Jarfino and drifts one further day at every
/// extra-weekly day after that.
/// </param>
/// <param name="DayOfSeason">Its place within its season, counting the Supertago where it falls.</param>
/// <param name="LastShift">
/// The most recent extra-weekly day before this one, back to the epoch, or null while there has
/// been none. Together with <paramref name="NextShift"/> it accounts for the gap between the two
/// weekdays: each extra-weekly day sets the Solstica count one day further back.
/// </param>
/// <param name="NextShift">The next extra-weekly day after this one, or null at the end of the window.</param>
public sealed record DayDetail(
    SolsticaDate Date,
    DateOnly? Gregorian,
    DayOfWeek? WeekDay,
    DayOfWeek? GregorianWeekDay,
    int DayOfYear,
    int DaysInYear,
    Season Season,
    int DayOfSeason,
    int SeasonLength,
    ValidityPeriod Period,
    SolsticaDate? LastShift,
    SolsticaDate? NextShift);

/// <summary>
/// A universal holiday of section 8.3 and how far off it is. <paramref name="DaysAway"/> counts
/// physical days, so it crosses the turn of the year and the extra-weekly days alike.
/// </summary>
public sealed record Festivity(SolsticaDate Date, string Name, DateOnly? Gregorian, int DaysAway);
