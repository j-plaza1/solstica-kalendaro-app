using System.ComponentModel;
using System.Globalization;
using SolsticaKalendaro.App.Resources.Strings;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>A row of the year view, ready to bind. One per row of the outline.</summary>
public abstract record RowView;

public sealed record SectionView(string Name, string Label, Color Dot) : RowView
{
    /// <summary>
    /// Esperanto names its months by their ordinal already, so "Unua · unua monato" says the
    /// same thing twice. Where a language has nothing to add, nothing is shown.
    /// </summary>
    public bool HasLabel => Label.Length > 0;
}

public sealed record WeekView(IReadOnlyList<DayView> Days) : RowView;

/// <param name="Note">What a day outside the week is, said under its name.</param>
public sealed record BandView(
    string Name, string Note, string Gregorian, Color Season, DateOnly? Date, SolsticaDate Solstica)
    : RowView;

/// <summary>
/// One day cell. Everything about it is fixed once built except <see cref="IsToday"/>, which
/// moves when the app is resumed on a later day. It notifies rather than being replaced, so
/// the highlight can move without rebuilding the list and throwing away the reader's place.
/// </summary>
public sealed class DayView(
    int column, string number, string gregorian, Color season, DateOnly? date, SolsticaDate solstica)
    : INotifyPropertyChanged
{
    /// <summary>Its place in the seven-column grid; the outline guarantees the order.</summary>
    public int Column { get; } = column;

    public string Number { get; } = number;
    public string Gregorian { get; } = gregorian;
    public Color Season { get; } = season;
    public DateOnly? Date { get; } = date;

    /// <summary>The day itself, for the detail screen. Gregorian dates run out; this one does not.</summary>
    public SolsticaDate Solstica { get; } = solstica;

    private bool _isToday;

    public bool IsToday
    {
        get => _isToday;
        set
        {
            if (_isToday == value) return;
            _isToday = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsToday)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// Turns an outline into rows a CollectionView can bind. Everything here is presentation:
/// which Gregorian month to repeat, what to call a block in Catalan, which colour a season
/// gets. No date is computed and no rule of the calendar is restated — the outline is asked.
/// </summary>
public static class YearView
{
    private static CultureInfo Culture => Language.Culture;

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
                        AppStrings.BandNoWeekday,
                        LongDate(band.Day.Gregorian),
                        palette[band.Day.Season],
                        band.Day.Gregorian,
                        band.Day.Date));
                    break;
            }
        }

        return rows;
    }

    /// <summary>
    /// Where a Gregorian date sits in the rows: which row to scroll to, and the cell to
    /// highlight. The cell is null when the date is an extra-weekly day, which is a row of
    /// its own rather than a cell in a week — there is somewhere to scroll, nothing to mark.
    /// </summary>
    public static (int Row, DayView? Cell) Locate(IReadOnlyList<RowView> rows, DateOnly date)
    {
        for (int i = 0; i < rows.Count; i++)
            switch (rows[i])
            {
                case WeekView week:
                    foreach (var day in week.Days)
                        if (day.Date == date) return (i, day);
                    break;

                case BandView band when band.Date == date:
                    return (i, null);
            }

        return (-1, null);
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
                day.Gregorian,
                day.Date)
            {
                IsToday = day.Gregorian == today
            };
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
            return Text.MonthLabel(nth);
        }

        int degrees = block.CardinalLongitude() ?? 0;
        return string.Format(Culture,
            block == PeriodKind.Jarmezo ? AppStrings.CentralPeriod : AppStrings.Transition, degrees);
    }
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
