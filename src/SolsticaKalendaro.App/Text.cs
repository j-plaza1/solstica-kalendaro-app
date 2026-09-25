using System.Globalization;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>
/// The Catalan the app puts around the core's facts. All of it lives here rather than in the
/// core, which states what is true of a day and never how to say it.
/// </summary>
public static class Text
{
    private static CultureInfo Culture => YearView.Culture;

    public static string WeekDay(DayOfWeek day) => Culture.DateTimeFormat.GetDayName(day);

    /// <summary>"12 Kvara", or just the name for a day that has no number of its own.</summary>
    public static string DayName(SolsticaDate date) =>
        date.Period.IsExtraWeekly() ? date.Period.Name() : $"{date.Day} {date.Period.Name()}";

    /// <summary>"dissabte, 1 d'abril de 2028".</summary>
    public static string FullDate(DateOnly date) => date.ToString("D", Culture);

    /// <summary>"20 de juny del 2028". Catalan's genitive month already carries its own "de".</summary>
    public static string LongDate(DateOnly? date) =>
        date is { } d ? $"{d.ToString("d MMMM", Culture)} del {d.Year}" : string.Empty;

    public static string Days(int count) => count == 1 ? "un dia" : $"{count} dies";

    /// <summary>"el Jarfino de 2027".</summary>
    public static string Shift(SolsticaDate? date) =>
        date is { } d ? $"el {d.Period.Name()} de {d.Year}" : "cap";

    public static string SeasonName(Season season) => season switch
    {
        Season.First => "primera estació",
        Season.Second => "segona estació",
        Season.Third => "tercera estació",
        Season.Fourth => "quarta estació",
        _ => throw new ArgumentOutOfRangeException(nameof(season))
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
            : $"{DayName(festivity.Date)} {festivity.Date.Year}";

        string gregorian = LongDate(festivity.Gregorian);

        return (day, gregorian) switch
        {
            ("", var when) => when,
            (var what, "") => what,
            var (what, when) => $"{what} · {when}"
        };
    }

    /// <summary>How far ahead of the day being looked at. Never "demà", which belongs to today.</summary>
    public static string After(int days) => days == 1 ? "l'endemà" : $"{days} dies després";

    /// <summary>
    /// How far from today, in the words someone would use out loud. Only today gets "demà" and
    /// "ahir": said of any other day they would be false.
    /// </summary>
    public static string FromToday(int days) => days switch
    {
        0 => "avui",
        1 => "demà",
        -1 => "ahir",
        > 1 => $"d'aquí a {days} dies",
        _ => $"fa {-days} dies"
    };
}
