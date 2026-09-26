using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>
/// When the calendar begins, as the reader has chosen it. Built like <see cref="Language"/>,
/// and for the same reason: one place decides, and everything else asks.
///
/// Choosing changes less than it looks. Every option but one puts 1 Unua on 21 December, and
/// two calendars anchored on the same day are the same calendar wherever they overlap, so
/// changing between them only removes or adds the years before the first one. The 2031 option
/// anchors on 22 December, and that one day is carried for ever.
/// </summary>
public static class CalendarStart
{
    private const string Preference = "calendar-begins";

    /// <summary>The expository date first, then the adoption windows in the order §7.6 lists them.</summary>
    public static readonly IReadOnlyList<SolsticaEpoch> Options =
        [SolsticaEpoch.Expository2026, .. SolsticaEpoch.AdoptionWindows];

    private static SolsticaEpoch? _chosen;
    private static SolsticaCalendar? _calendar;

    public static SolsticaEpoch Chosen => _chosen ??= Stored();

    /// <summary>
    /// The one calendar the app converts with. Rebuilt only when the choice changes, so that
    /// nothing else is ever tempted to make an epoch of its own.
    /// </summary>
    public static SolsticaCalendar Calendar => _calendar ??= new SolsticaCalendar(Chosen);

    public static void Choose(SolsticaEpoch epoch)
    {
        Preferences.Set(Preference, epoch.FirstSolsticaYear);
        _chosen = epoch;
        _calendar = null;
    }

    /// <summary>
    /// What was saved, or the expository date. An unknown year means a preference written by a
    /// version that offered something this one does not, which is not worth crashing over.
    /// </summary>
    private static SolsticaEpoch Stored()
    {
        int year = Preferences.Get(Preference, SolsticaEpoch.Expository2026.FirstSolsticaYear);
        return Options.FirstOrDefault(e => e.FirstSolsticaYear == year) ?? SolsticaEpoch.Expository2026;
    }
}
