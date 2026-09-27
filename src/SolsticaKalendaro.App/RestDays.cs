using System.Globalization;

namespace SolsticaKalendaro.App;

/// <summary>
/// The days of the week the reader rests on, as they have chosen them. Built like
/// <see cref="Language"/> and <see cref="CalendarStart"/>: one place decides, and everything
/// else asks.
///
/// They are days of the <i>Solstica</i> week. After the first Jarfino that is not the same
/// physical day as the Gregorian Saturday, which is the whole practical consequence of the
/// proposal, and the point of choosing them here rather than assuming them.
/// </summary>
public static class RestDays
{
    private const string Preference = "rest-days";

    /// <summary>What most readers rest on, until they say otherwise.</summary>
    private static readonly DayOfWeek[] Weekend = [DayOfWeek.Saturday, DayOfWeek.Sunday];

    private static IReadOnlySet<DayOfWeek>? _chosen;

    public static IReadOnlySet<DayOfWeek> Chosen => _chosen ??= Stored();

    /// <summary>
    /// On or off. Choosing none is allowed: a calendar of a reader who rests on no fixed day
    /// still has its own festivities, and saying so is not this app's business.
    /// </summary>
    public static void Toggle(DayOfWeek day)
    {
        var days = new HashSet<DayOfWeek>(Chosen);
        if (!days.Add(day)) days.Remove(day);

        Preferences.Set(Preference, string.Join(',', days.Select(d => ((int)d).ToString(CultureInfo.InvariantCulture))));
        _chosen = days;
    }

    /// <summary>
    /// What was saved. An empty string is a reader who chose none; anything unreadable is a
    /// preference this version does not understand, which is not worth crashing over.
    /// </summary>
    private static IReadOnlySet<DayOfWeek> Stored()
    {
        if (Preferences.Get(Preference, null) is not { } saved) return new HashSet<DayOfWeek>(Weekend);
        if (saved.Length == 0) return new HashSet<DayOfWeek>();

        var days = new HashSet<DayOfWeek>();
        foreach (string part in saved.Split(','))
        {
            if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int day)
                || day is < 0 or > 6)
                return new HashSet<DayOfWeek>(Weekend);

            days.Add((DayOfWeek)day);
        }

        return days;
    }
}
