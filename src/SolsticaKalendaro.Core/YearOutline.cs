namespace SolsticaKalendaro.Core;

/// <summary>
/// One row of a year, in the order an interface paints them from top to bottom.
///
/// The outline carries structure only: where each day falls, what it is, and which
/// Gregorian date it answers to. Formatting is not here — when to repeat the Gregorian
/// month, how to name a weekday, what to abbreviate — because it depends on the culture
/// and on the space available, and both belong to the interface.
/// </summary>
public abstract record OutlineRow;

/// <summary>
/// Opens a month or a transition block. The extra-weekly days open nothing: they belong
/// to no block of their own, so the section they interrupt simply resumes after them.
/// </summary>
public sealed record SectionHeader(PeriodKind Block) : OutlineRow;

/// <summary>
/// Seven days, Monday to Sunday, all inside one block. A week never spans two blocks:
/// every block is a whole number of weeks and every block opens on a Monday, in every
/// validity period (section 8.3).
/// </summary>
public sealed record WeekRow(PeriodKind Block, IReadOnlyList<OutlineDay> Days) : OutlineRow;

/// <summary>
/// A Jarfino or a Supertago. These pause the seven-day cycle rather than belonging to it,
/// so they stand on their own rather than inside a <see cref="WeekRow"/>, and they have no
/// weekday to align under.
/// </summary>
public sealed record ExtraWeeklyRow(SolsticaDate Date) : OutlineRow;

/// <summary>One day of a <see cref="WeekRow"/>.</summary>
/// <param name="Gregorian">
/// Null where the day has no Gregorian counterpart .NET can represent. Solstica year 10000
/// runs past <see cref="DateOnly.MaxValue"/>, so its last days convert to nothing; see
/// <see cref="SolsticaCalendar.CanConvert"/>.
/// </param>
public sealed record OutlineDay(SolsticaDate Date, DateOnly? Gregorian, Season Season, bool IsFestivity);
