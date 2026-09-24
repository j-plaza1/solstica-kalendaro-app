using System.Windows.Input;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

public partial class MainPage : ContentPage
{
    private static readonly SolsticaCalendar Cal = new(SolsticaEpoch.Expository2026);

    /// <summary>
    /// Opens a day. Pushing rather than replacing is what keeps the year where the reader left
    /// it: this page stays alive behind the detail, scroll position and all.
    /// </summary>
    public ICommand OpenDay { get; }

    private IReadOnlyList<RowView> _rows = [];
    private DayView? _highlighted;
    private int _todayRow = -1;
    private int _shownYear;

    public MainPage()
    {
        InitializeComponent();
        OpenDay = new Command<SolsticaDate>(date => Navigation.PushAsync(new DayDetailPage(Cal, date)));
        NameTheWeekdays();
        Show(Today());

        // The first attempt scrolled from the constructor and landed a month short: the
        // CollectionView had no native view yet, so the request went nowhere useful.
        // Loaded is the event that says it does. Every row has a fixed height, so scrolling
        // by index is exact whether or not the rows have been realised.
        Rows.Loaded += ScrollToTodayOnce;

        Loaded += WatchForResume;
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);

    private void Show(DateOnly today)
    {
        // The calendar begins at its epoch. Before that there is no year to be in, so the
        // view opens on the first year it covers and says how long the wait is.
        var outline = Cal.Outline(Cal.Epoch.FirstSolsticaYear);
        var opens = FirstGregorianOf(outline);
        _shownYear = Cal.Epoch.FirstSolsticaYear;

        if (opens is { } start && today < start)
        {
            int days = start.DayNumber - today.DayNumber;
            SubtitleLabel.Text = $"Encara no ha començat · falten {days} {(days == 1 ? "dia" : "dies")}";
            SubtitleLabel.IsVisible = true;
        }
        else
        {
            _shownYear = Cal.FromGregorian(today).Year;
            outline = Cal.Outline(_shownYear);
            SubtitleLabel.IsVisible = false;
        }

        YearLabel.Text = _shownYear.ToString(YearView.Culture);

        _rows = YearView.Build(outline, today, Palette());
        Rows.ItemsSource = _rows;

        (_todayRow, _highlighted) = YearView.Locate(_rows, today);

        // No today to go to in a year the calendar has not reached yet.
        TodayButton.IsEnabled = _todayRow >= 0;
        TodayButton.Opacity = _todayRow >= 0 ? 1 : 0.4;
    }

    private void ScrollToTodayOnce(object? sender, EventArgs e)
    {
        Rows.Loaded -= ScrollToTodayOnce;
        GoToToday(animate: false);
    }

    private void OnTodayClicked(object? sender, EventArgs e) => GoToToday(animate: true);

    private void GoToToday(bool animate)
    {
        if (_todayRow >= 0) Rows.ScrollTo(_todayRow, position: ScrollToPosition.Center, animate: animate);
    }

    // ---------- coming back on a later day ----------

    private void WatchForResume(object? sender, EventArgs e)
    {
        Loaded -= WatchForResume;
        if (Window is not null) Window.Resumed += OnResumed;
    }

    /// <summary>
    /// The app may have sat in the background across midnight. Move the highlight to the new
    /// day, but leave the scroll alone: the reader was looking at some part of the year, and
    /// it is not for us to decide they meant to stop.
    /// </summary>
    private void OnResumed(object? sender, EventArgs e)
    {
        var today = Today();
        if (_highlighted?.Date == today) return;

        var (row, cell) = YearView.Locate(_rows, today);
        if (row < 0)
        {
            // A new Solstica year, or the epoch has arrived: the rows themselves are stale.
            Show(today);
            GoToToday(animate: false);
            return;
        }

        if (_highlighted is not null) _highlighted.IsToday = false;
        if (cell is not null) cell.IsToday = true;

        _todayRow = row;
        _highlighted = cell;
        TodayButton.IsEnabled = true;
        TodayButton.Opacity = 1;
    }

    /// <summary>
    /// The Gregorian date the year opens on, taken from the outline rather than worked out
    /// here. Null only past what DateOnly can represent, which the first day never is.
    /// </summary>
    private static DateOnly? FirstGregorianOf(IReadOnlyList<OutlineRow> outline) =>
        outline.OfType<WeekRow>().FirstOrDefault()?.Days[0].Gregorian;

    /// <summary>
    /// Monday to Sunday: the Solstica week, which every block of the year begins on. It is not
    /// the Gregorian week of the same days, and after the first Jarfino the two do not agree.
    /// </summary>
    private void NameTheWeekdays()
    {
        Label[] slots = [Weekday0, Weekday1, Weekday2, Weekday3, Weekday4, Weekday5, Weekday6];
        var names = YearView.Culture.DateTimeFormat.AbbreviatedDayNames;

        for (int i = 0; i < slots.Length; i++)
        {
            var day = (DayOfWeek)(((int)DayOfWeek.Monday + i) % 7);
            slots[i].Text = names[(int)day].TrimEnd('.').ToUpper(YearView.Culture);
            slots[i].FontFamily = "PlexSemiBold";
            slots[i].FontSize = 10.5;
            slots[i].CharacterSpacing = 0.6;
            slots[i].HorizontalTextAlignment = TextAlignment.Center;
            slots[i].TextColor = (Color)Application.Current!.Resources["Muted"];
        }
    }

    private YearPalette Palette() => new(
        (Color)Application.Current!.Resources["SeasonFirst"],
        (Color)Application.Current!.Resources["SeasonSecond"],
        (Color)Application.Current!.Resources["SeasonThird"],
        (Color)Application.Current!.Resources["SeasonFourth"]);
}
