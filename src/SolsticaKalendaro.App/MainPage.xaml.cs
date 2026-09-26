using System.Windows.Input;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

public partial class MainPage : ContentPage
{
    private static readonly SolsticaCalendar Cal = new(SolsticaEpoch.Expository2026);

    /// <summary>The years this calendar has. Below the first there is no calendar to show.</summary>
    private static int FirstYear => Cal.Epoch.FirstSolsticaYear;
    private static int LastYear => ValidityPeriod.Table[^1].LastYear;

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
        StyleYearControls();

        ShowYear(YearOf(Today()) ?? FirstYear);

        // The first attempt scrolled from the constructor and landed a month short: the
        // CollectionView had no native view yet, so the request went nowhere useful.
        // Loaded is the event that says it does. Every row has a fixed height, so scrolling
        // by index is exact whether or not the rows have been realised.
        Rows.Loaded += ScrollToTodayOnce;

        Loaded += WatchForResume;
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>The Solstica year a Gregorian date falls in, or null before the calendar starts.</summary>
    private static int? YearOf(DateOnly date) => Cal.IsInRange(FirstYear) && date >= Cal.Epoch.AdoptionDate
        ? Cal.FromGregorian(date).Year
        : null;

    // ---------- the year on screen ----------

    private void ShowYear(int year)
    {
        _shownYear = Math.Clamp(year, FirstYear, LastYear);
        var today = Today();

        YearButton.Text = $"{_shownYear.ToString(YearView.Culture)} ▾";
        PreviousYearButton.IsEnabled = _shownYear > FirstYear;
        NextYearButton.IsEnabled = _shownYear < LastYear;
        PreviousYearButton.Opacity = PreviousYearButton.IsEnabled ? 1 : 0.3;
        NextYearButton.Opacity = NextYearButton.IsEnabled ? 1 : 0.3;

        ShowPeriodLine(today);

        _rows = YearView.Build(Cal.Outline(_shownYear), today, Palette());
        Rows.ItemsSource = _rows;

        (_todayRow, _highlighted) = YearView.Locate(_rows, today);

        // No today to go to in a year that does not contain it.
        bool reachable = YearOf(today) is not null;
        TodayButton.IsEnabled = reachable;
        TodayButton.Opacity = reachable ? 1 : 0.4;
    }

    /// <summary>
    /// The line under the header. While the calendar has not begun it carries the wait, because
    /// that is the fact a reader needs before any period means anything to them.
    /// </summary>
    private void ShowPeriodLine(DateOnly today)
    {
        var opens = Cal.Epoch.AdoptionDate;
        bool begun = today >= opens;

        PeriodButton.IsVisible = begun;
        LeapLabel.IsVisible = begun && SolsticaCalendar.IsLeapYear(_shownYear);
        SubtitleLabel.IsVisible = !begun;

        if (begun)
        {
            var period = SolsticaCalendar.PeriodFor(_shownYear);
            PeriodButton.Text = $"Període {period.FirstYear}–{period.LastYear} ›";
        }
        else
        {
            int days = opens.DayNumber - today.DayNumber;
            SubtitleLabel.Text = $"Encara no ha començat · falten {days} {(days == 1 ? "dia" : "dies")}";
        }
    }

    private void OnPreviousYear(object? sender, EventArgs e) => ShowYear(_shownYear - 1);

    private void OnNextYear(object? sender, EventArgs e) => ShowYear(_shownYear + 1);

    private void OnYearClicked(object? sender, EventArgs e) => Navigation.PushAsync(new GoToPage());

    private void OnPeriodClicked(object? sender, EventArgs e) =>
        Navigation.PushAsync(new PlaceholderPage("Període"));

    private async void OnMenuClicked(object? sender, EventArgs e)
    {
        string choice = await DisplayActionSheetAsync(null, "Tanca", null,
            "Opcions", "Com es llegeix", "Quant a");

        if (choice is "Opcions" or "Com es llegeix" or "Quant a")
            await Navigation.PushAsync(new PlaceholderPage(choice));
    }

    // ---------- today ----------

    private void ScrollToTodayOnce(object? sender, EventArgs e)
    {
        Rows.Loaded -= ScrollToTodayOnce;
        GoToToday(animate: false);
    }

    /// <summary>Today is in some year, which may not be the one on screen.</summary>
    private void OnTodayClicked(object? sender, EventArgs e)
    {
        if (YearOf(Today()) is not { } year) return;

        if (year != _shownYear) ShowYear(year);
        GoToToday(animate: true);
    }

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
            // Today is in another year, or the epoch has arrived: the rows themselves are stale.
            ShowYear(YearOf(today) ?? _shownYear);
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

    // ---------- appearance ----------

    private void StyleYearControls()
    {
        YearButton.FontFamily = "SpectralSemiBold";
        YearButton.FontSize = 23;
        YearButton.TextColor = (Color)Application.Current!.Resources["Ink"];
        YearButton.BackgroundColor = Colors.Transparent;
        YearButton.BorderWidth = 0;
        YearButton.Padding = new Thickness(6, 0);
        YearButton.MinimumHeightRequest = 44;

        foreach (var arrow in new[] { PreviousYearButton, NextYearButton })
        {
            arrow.FontFamily = "Plex";
            arrow.FontSize = 22;
            arrow.TextColor = (Color)Application.Current!.Resources["Muted"];
            arrow.BackgroundColor = Colors.Transparent;
            arrow.BorderWidth = 0;
            arrow.Padding = new Thickness(0);
            arrow.MinimumHeightRequest = 44;
            arrow.MinimumWidthRequest = 44;
        }
    }

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
