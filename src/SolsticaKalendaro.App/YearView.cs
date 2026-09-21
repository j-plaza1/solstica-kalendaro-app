using System.Globalization;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>A row of the year view, ready to bind. One per row of the outline.</summary>
public abstract record RowView;

public sealed record SectionView(string Name, string Label, Color Dot) : RowView;

public sealed record WeekView(IReadOnlyList<DayView> Days) : RowView;

public sealed record BandView(string Name, string Gregorian, Color Season) : RowView;

/// <param name="Column">Its place in the seven-column grid; the outline guarantees the order.</param>
public sealed record DayView(int Column, string Number, string Gregorian, Color Season, bool IsToday);

/// <summary>
/// Turns an outline into rows a CollectionView can bind. Everything here is presentation:
/// which Gregorian month to repeat, what to call a block in Catalan, which colour a season
/// gets. No date is computed and no rule of the calendar is restated — the outline is asked.
/// </summary>
public static class YearView
{
    /// <summary>The app speaks Catalan, so it formats dates in Catalan whatever the device is set to.</summary>
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("ca-ES");

    public static IReadOnlyList<RowView> Build(IReadOnlyList<OutlineRow> outline, DateOnly today, YearPalette palette)
    {
        var rows = new List<RowView>(outline.Count);

        for (int i = 0; i < outline.Count; i++)
        {
            switch (outline[i])
            {
                case SectionHeader header:
                    // The header carries no day, so its colour comes from the block it opens:
                    // the first day of the week that follows it.
                    var opens = ((WeekView)Project((WeekRow)outline[i + 1], today, palette)).Days[0];
                    rows.Add(new SectionView(header.Block.Name(), LabelFor(header.Block), opens.Season));
                    break;

                case WeekRow week:
                    rows.Add(Project(week, today, palette));
                    break;

                case ExtraWeeklyRow band:
                    rows.Add(new BandView(
                        band.Day.Date.Period.Name(),
                        LongDate(band.Day.Gregorian),
                        palette[band.Day.Season]));
                    break;
            }
        }

        return rows;
    }

    /// <summary>Index of the row holding today, or -1 when the year does not contain it.</summary>
    public static int IndexOfToday(IReadOnlyList<RowView> rows)
    {
        for (int i = 0; i < rows.Count; i++)
            if (rows[i] is WeekView week && week.Days.Any(d => d.IsToday))
                return i;
        return -1;
    }

    private static RowView Project(WeekRow week, DateOnly today, YearPalette palette)
    {
        var days = new DayView[week.Days.Count];
        for (int c = 0; c < week.Days.Count; c++)
        {
            var day = week.Days[c];
            days[c] = new DayView(
                c,
                day.Date.Day.ToString(Culture),
                // The month is worth repeating where the eye needs it: at the start of a row,
                // and wherever a Gregorian month turns over mid-row.
                ShortDate(day.Gregorian, withMonth: c == 0 || day.Gregorian?.Day == 1),
                palette[day.Season],
                day.Gregorian == today);
        }
        return new WeekView(days);
    }

    private static string ShortDate(DateOnly? date, bool withMonth) => date switch
    {
        null => string.Empty,                                   // beyond what DateOnly can represent
        { } d when withMonth => $"{d.Day} {MonthName(Culture.DateTimeFormat.AbbreviatedMonthNames, d.Month)}",
        { } d => d.Day.ToString(Culture)
    };

    private static string LongDate(DateOnly? date) =>
        date is { } d ? $"{d.Day} {MonthName(Culture.DateTimeFormat.MonthNames, d.Month)}" : string.Empty;

    /// <summary>
    /// A bare month name. Catalan's date patterns take the genitive, which drags a "de" along
    /// that a calendar cell has no room for and does not need beside a number.
    /// </summary>
    private static string MonthName(string[] names, int month)
    {
        string name = names[month - 1].TrimEnd('.');
        if (name.StartsWith("de ", StringComparison.OrdinalIgnoreCase)) name = name[3..];
        if (name.StartsWith("d'", StringComparison.OrdinalIgnoreCase)) name = name[2..];
        return name;
    }

    /// <summary>
    /// The short note beside a block's name. A month is named by its place among the months;
    /// a transition block by the cardinal point it is built for, which the core knows.
    /// </summary>
    private static string LabelFor(PeriodKind block)
    {
        if (block.IsMonth())
        {
            int nth = Array.FindIndex(YearLayout.Sequence.Where(p => p.IsMonth()).ToArray(), p => p == block);
            return $"{Ordinals[nth]} mes";
        }

        int degrees = block.CardinalLongitude() ?? 0;
        return block == PeriodKind.Jarmezo
            ? $"període central · {degrees}°"
            : $"transició · {degrees}°";
    }

    private static readonly string[] Ordinals =
    [
        "primer", "segon", "tercer", "quart", "cinquè", "sisè",
        "setè", "vuitè", "novè", "desè", "onzè", "dotzè"
    ];
}

/// <summary>The four season colours, read from the page's resources so the XAML owns them.</summary>
public sealed class YearPalette(Color first, Color second, Color third, Color fourth)
{
    public Color this[Season season] => season switch
    {
        Season.First => first,
        Season.Second => second,
        Season.Third => third,
        Season.Fourth => fourth,
        _ => throw new ArgumentOutOfRangeException(nameof(season))
    };
}
