using System.Globalization;
using SolsticaKalendaro.App.Resources.Strings;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>
/// The words the app puts around the core's facts. The strings themselves live in the resource
/// files, in four languages; what is here is the choosing between them — singular or plural,
/// today or some other day — which is a decision and not a translation.
/// </summary>
public static class Text
{
    private static CultureInfo Culture => Language.Culture;

    public static string WeekDay(DayOfWeek day) => Culture.DateTimeFormat.GetDayName(day);

    /// <summary>
    /// "12 Kvara", or just the name for a day that has no number of its own. The block's name
    /// is never translated: it is what the calendar calls it.
    /// </summary>
    public static string DayName(SolsticaDate date) =>
        date.Period.IsExtraWeekly() ? date.Period.Name() : $"{date.Day} {date.Period.Name()}";

    public static string FullDate(DateOnly date) => date.ToString("D", Culture);

    /// <summary>
    /// The day and its month, with the year. Catalan's genitive month carries its own "de"; the
    /// other three do not, and none of them mind it being asked for.
    /// </summary>
    public static string LongDate(DateOnly? date) =>
        date is { } d
            ? string.Format(Culture, AppStrings.DateWithYear, d.ToString("d MMMM", Culture), d.Year)
            : string.Empty;

    /// <summary>"el Jarfino de 2027": the article is the language's, the name is the calendar's.</summary>
    public static string Shift(SolsticaDate? date) =>
        date is { } d ? string.Format(Culture, AppStrings.ShiftName, d.Period.Name(), d.Year) : string.Empty;

    public static string SeasonName(Season season) => season switch
    {
        Season.First => AppStrings.Season1,
        Season.Second => AppStrings.Season2,
        Season.Third => AppStrings.Season3,
        Season.Fourth => AppStrings.Season4,
        _ => throw new ArgumentOutOfRangeException(nameof(season))
    };

    /// <summary>The note beside a month's name: its place among the twelve.</summary>
    public static string MonthLabel(int nth) => nth switch
    {
        0 => AppStrings.Month1, 1 => AppStrings.Month2, 2 => AppStrings.Month3,
        3 => AppStrings.Month4, 4 => AppStrings.Month5, 5 => AppStrings.Month6,
        6 => AppStrings.Month7, 7 => AppStrings.Month8, 8 => AppStrings.Month9,
        9 => AppStrings.Month10, 10 => AppStrings.Month11, 11 => AppStrings.Month12,
        _ => throw new ArgumentOutOfRangeException(nameof(nth))
    };

    /// <summary>
    /// "8 Jarmezo · 21 de juny del 2028". The Gregorian date always carries its year: around the
    /// turn of the year the two disagree, and 1 Unua 2028 falling on 21 December 2027 is exactly
    /// the sort of thing a reader should not have to work out.
    /// </summary>
    public static string FestivityWhen(Festivity festivity, int fromYear)
    {
        // A Jarfino or a Supertago IS its name, and the name is already the heading above.
        // Its year is the Gregorian one too, so the date below carries it: only a day with a
        // number of its own needs its Solstica year saying, and only when it is not this one.
        string day = festivity.Date.Period.IsExtraWeekly() ? string.Empty
            : festivity.Date.Year == fromYear ? DayName(festivity.Date)
            : $"{DayName(festivity.Date)} {festivity.Date.Year.ToString(Culture)}";

        string gregorian = LongDate(festivity.Gregorian);

        return (day, gregorian) switch
        {
            ("", var when) => when,
            (var what, "") => what,
            var (what, when) => $"{what} · {when}"
        };
    }

    /// <summary>How far ahead of the day being looked at. Never "tomorrow", which is today's.</summary>
    public static string After(int days) =>
        days == 1 ? AppStrings.AfterOne : string.Format(Culture, AppStrings.AfterMany, days);

    /// <summary>
    /// How far from today, in the words someone would use out loud. Only today gets "tomorrow"
    /// and "yesterday": said of any other day they would be false.
    /// </summary>
    public static string FromToday(int days) => days switch
    {
        0 => AppStrings.FromTodayToday,
        1 => AppStrings.FromTodayTomorrow,
        -1 => AppStrings.FromTodayYesterday,
        > 1 => string.Format(Culture, AppStrings.FromTodayIn, days),
        _ => string.Format(Culture, AppStrings.FromTodayAgo, -days)
    };
}
