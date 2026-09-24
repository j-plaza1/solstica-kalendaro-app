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

    /// <summary>"20 de juny de 2028". Catalan's genitive month already carries its own "de".</summary>
    public static string LongDate(DateOnly? date) =>
        date is { } d ? $"{d.ToString("d MMMM", Culture)} de {d.Year}" : string.Empty;

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
    /// "8 Jarmezo · 21 de juny". The year is named only when the festivity falls in a later one,
    /// where leaving it out would be a small lie about which year is being looked at.
    /// </summary>
    public static string FestivityWhen(Festivity festivity, int fromYear)
    {
        bool sameYear = festivity.Date.Year == fromYear;

        // An extra-weekly day IS its name, and the name is already the heading above this line.
        string day = festivity.Date.Period.IsExtraWeekly()
            ? (sameYear ? string.Empty : festivity.Date.Year.ToString(Culture))
            : (sameYear ? DayName(festivity.Date) : $"{DayName(festivity.Date)} {festivity.Date.Year}");

        string gregorian = festivity.Gregorian is { } g ? g.ToString("d MMMM", Culture) : string.Empty;

        return (day, gregorian) switch
        {
            ("", var when) => when,
            (var what, "") => what,
            var (what, when) => $"{what} · {when}"
        };
    }

    public static string Away(int days) => days == 1 ? "demà" : $"d'aquí a {days} dies";
}
