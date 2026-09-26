using System.Windows.Input;
using SolsticaKalendaro.App.Resources.Strings;
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
        NameEverything();
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

        YearButton.Text = _shownYear.ToString(Language.Culture);
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
    /// The line under the header: the period of the year on screen, always, and the way into
    /// what that means. What comes before it is either nothing, or the one thing worth saying
    /// about this year — that it is long, or that the calendar has not reached it yet.
    ///
    /// The link is last because "› ·" would put two separators together.
    /// </summary>
    private void ShowPeriodLine(DateOnly today)
    {
        var period = SolsticaCalendar.PeriodFor(_shownYear);
        PeriodButton.Text = string.Format(
            Language.Culture, AppStrings.Period, period.FirstYear, period.LastYear);

        var opens = Cal.Epoch.AdoptionDate;
        int days = opens.DayNumber - today.DayNumber;

        string? prefix =
            _shownYear == FirstYear && today < opens
                ? (days == 1 ? AppStrings.StartsOne : string.Format(Language.Culture, AppStrings.StartsMany, days))
            : SolsticaCalendar.IsLeapYear(_shownYear) ? AppStrings.LeapYear
            : null;

        PrefixLabel.IsVisible = prefix is not null;
        if (prefix is not null) PrefixLabel.Text = $"{prefix} · ";
    }

    private void OnPreviousYear(object? sender, EventArgs e) => ShowYear(_shownYear - 1);

    private void OnNextYear(object? sender, EventArgs e) => ShowYear(_shownYear + 1);

    private void OnYearClicked(object? sender, EventArgs e) => Navigation.PushAsync(new GoToPage());

    private void OnPeriodClicked(object? sender, EventArgs e) =>
        Navigation.PushAsync(new PlaceholderPage(AppStrings.TabPeriod));

    // ---------- the menu ----------

    private void OnMenuClicked(object? sender, EventArgs e) => MenuOverlay.IsVisible = true;

    private void OnDismissMenu(object? sender, TappedEventArgs e) => MenuOverlay.IsVisible = false;

    private void OnOptionsClicked(object? sender, EventArgs e)
    {
        MenuOverlay.IsVisible = false;
        Navigation.PushAsync(new OptionsPage());
    }

    private void OnHowToReadClicked(object? sender, EventArgs e) =>
        OpenFromMenu(AppStrings.MenuHowToRead);

    private void OnAboutClicked(object? sender, EventArgs e) => OpenFromMenu(AppStrings.MenuAbout);

    private void OpenFromMenu(string title)
    {
        MenuOverlay.IsVisible = false;
        Navigation.PushAsync(new PlaceholderPage(title));
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
        YearButton.Padding = new Thickness(6, 0, 24, 0);
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

            // Equal widths, so the year sits between equal gaps rather than nearer one arrow.
            arrow.WidthRequest = 38;
        }

        foreach (var item in new[] { OptionsItem, HowToReadItem, AboutItem }) StyleMenuItem(item);
    }

    /// <summary>The menu reads as a list of things to do, so its text is the ordinary ink.</summary>
    private static void StyleMenuItem(Button item)
    {
        item.FontFamily = "Plex";
        item.FontSize = 15;
        item.TextColor = (Color)Application.Current!.Resources["Ink"];
        item.BackgroundColor = Colors.Transparent;
        item.BorderWidth = 0;
        item.Padding = new Thickness(16, 10);
        item.MinimumHeightRequest = 44;
        item.HorizontalOptions = LayoutOptions.Start;
    }

    /// <summary>
    /// Monday to Sunday: the Solstica week, which every block of the year begins on. It is not
    /// the Gregorian week of the same days, and after the first Jarfino the two do not agree.
    /// </summary>
    private void NameEverything()
    {
        TodayButton.Text = AppStrings.Today;
        OptionsItem.Text = AppStrings.MenuOptions;
        HowToReadItem.Text = AppStrings.MenuHowToRead;
        AboutItem.Text = AppStrings.MenuAbout;

        SemanticProperties.SetHint(PreviousYearButton, AppStrings.HintPreviousYear);
        SemanticProperties.SetHint(NextYearButton, AppStrings.HintNextYear);
        SemanticProperties.SetHint(YearButton, AppStrings.HintYear);
        SemanticProperties.SetHint(TodayButton, AppStrings.HintToday);
        SemanticProperties.SetHint(MenuButton, AppStrings.HintMenu);

        Label[] slots = [Weekday0, Weekday1, Weekday2, Weekday3, Weekday4, Weekday5, Weekday6];
        var names = Language.Culture.DateTimeFormat.AbbreviatedDayNames;

        for (int i = 0; i < slots.Length; i++)
        {
            var day = (DayOfWeek)(((int)DayOfWeek.Monday + i) % 7);
            slots[i].Text = names[(int)day].TrimEnd('.').ToUpper(Language.Culture);
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
