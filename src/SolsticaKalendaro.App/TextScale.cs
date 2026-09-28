using System.Globalization;

namespace SolsticaKalendaro.App;

/// <summary>
/// How much larger the reader has asked every app's text to be. The app has no size setting of
/// its own: this is a system preference, already set by whoever needs it, and it applies to
/// every app alike.
///
/// Text screens simply follow it, without limit. The year is a seven-column grid, which cannot
/// grow past the width of the screen, so <see cref="Grid"/> stops at 160 % and the cells drop
/// the Gregorian month above 130 %, where there is no longer room for it.
///
/// **Read per platform**, because MAUI exposes no cross-platform way to ask: each system keeps
/// the setting in its own place, and the app cannot follow what it cannot read. The only other
/// per-platform code is the one that paints the system bars, for the same reason.
/// </summary>
public static class TextScale
{
    /// <summary>Beyond this the seven columns would be wider than the screen.</summary>
    private const double GridLimit = 1.6;

    /// <summary>Above this a cell has room for the day of the month and nothing else.</summary>
    private const double MonthLimit = 1.3;

    /// <summary>The system's own setting, where 1.0 is the ordinary size. Read afresh, because
    /// the reader can change it while the app is in the background.</summary>
    public static double System => Read();

    /// <summary>The scale the year view is drawn at: the system's, up to the limit.</summary>
    public static double Grid => Math.Clamp(System, 1.0, GridLimit);

    /// <summary>Whether a cell's Gregorian line still has room for the month's name.</summary>
    public static bool GridShowsMonth => Grid <= MonthLimit;

    /// <summary>
    /// The scale the row heights and grid sizes in <see cref="Application.Resources"/> were
    /// built at. The rows must keep fixed heights — scrolling to a day by index depends on it —
    /// so they are computed once and the pages are rebuilt if the setting changes underneath.
    /// </summary>
    public static double Applied { get; private set; } = 1.0;

    /// <summary>
    /// Scales the grid's sizes, in place, before any page is built. Each key holds the size at
    /// the ordinary text size; heights are rounded to whole units, because a row of 58.4 is a
    /// row whose position cannot be computed exactly.
    /// </summary>
    public static void Apply(ResourceDictionary resources)
    {
        double scale = Grid;
        Applied = scale;

        foreach (string key in Heights) resources[key] = Math.Round(Base(resources, key) * scale);
        foreach (string key in Sizes) resources[key] = Base(resources, key) * scale;
    }

    /// <summary>True when the reader has changed the setting since the sizes were built.</summary>
    public static bool HasChanged => Math.Abs(Grid - Applied) > 0.001;

    private static readonly string[] Heights =
        ["RowSectionHeight", "RowWeekHeight", "RowBandHeight", "TodayCircleSize"];

    private static readonly string[] Sizes =
    [
        "SizeSectionName", "SizeSectionLabel", "SizeDayNumber", "SizeDayGregorian",
        "SizeBandName", "SizeBandNote", "SizeBandDate", "SizeWeekdayInitial"
    ];

    /// <summary>
    /// The unscaled value, kept aside the first time so that applying a second scale does not
    /// compound on the first.
    /// </summary>
    private static double Base(ResourceDictionary resources, string key)
    {
        string kept = key + "Base";

        if (!resources.TryGetValue(kept, out object? value))
        {
            value = resources[key];
            resources[kept] = value;
        }

        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private static double Read()
    {
#if ANDROID
        // Resources.Configuration.FontScale is the "Font size" slider of the system settings.
        return global::Android.App.Application.Context.Resources?.Configuration?.FontScale ?? 1.0;
#elif IOS || MACCATALYST
        // Dynamic Type, asked the only way it can be: what one point scales to.
        return UIKit.UIFontMetrics.DefaultMetrics.GetScaledValue(1);
#elif WINDOWS
        return new global::Windows.UI.ViewManagement.UISettings().TextScaleFactor;
#else
        return 1.0;
#endif
    }
}
