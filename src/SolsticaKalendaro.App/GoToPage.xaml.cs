using SolsticaKalendaro.App.Resources.Strings;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>
/// The one panel behind every way of moving about: a year typed, a period chosen, or a day
/// named in either calendar. All of them land the same way — on a year, with the panel closed
/// behind them — and the Date tab lands on the day itself, marked.
///
/// The panel knows which year is on screen and how to ask for another; it holds no calendar of
/// its own and decides nothing about what the year view then does.
/// </summary>
public partial class GoToPage : ContentPage
{
    private readonly int _shownYear;
    private readonly Action<int> _goToYear;
    private readonly Action<SolsticaDate> _goToDate;

    private readonly YearField _year;
    private readonly YearField _dateYear;

    private Button _selected;
    private Button _calendar;

    /// <summary>The day the Date tab is holding, whichever side is showing it.</summary>
    private SolsticaDate _chosen;

    /// <summary>
    /// A Gregorian date was picked from before the calendar begins. It names no Solstica day,
    /// so <see cref="_chosen"/> still holds the last one that did; what this changes is that
    /// there is nowhere to go, and a sentence saying why.
    /// </summary>
    private bool _beforeStart;

    /// <summary>The blocks of the year on the Solstica side, in the order they come.</summary>
    private IReadOnlyList<(PeriodKind Kind, int Days)> _blocks = [];

    /// <summary>
    /// The pickers are being set to match the day, rather than being used to change it. Their
    /// events fire either way and would otherwise read a half-written state as a choice.
    /// </summary>
    private bool _filling;

    /// <param name="shownYear">The year the reader came from: what the fields hold, and which
    /// period is ticked.</param>
    /// <param name="goToYear">How to ask the year view for a year, once this page is gone.</param>
    /// <param name="goToDate">The same, for a single day, which arrives marked.</param>
    public GoToPage(int shownYear, Action<int> goToYear, Action<SolsticaDate> goToDate)
    {
        _shownYear = shownYear;
        _goToYear = goToYear;
        _goToDate = goToDate;

        InitializeComponent();

        SemanticProperties.SetHint(BackButton, AppStrings.HintBackToYear);
        TitleLabel.Text = AppStrings.GoTo;
        YearTab.Text = AppStrings.TabYear;
        PeriodTab.Text = AppStrings.TabPeriod;
        DateTab.Text = AppStrings.TabDate;

        GoButton.Text = AppStrings.GoButton;
        DateGoButton.Text = AppStrings.GoButton;
        GregorianButton.Text = AppStrings.CalendarGregorian;
        SolsticaButton.Text = AppStrings.CalendarSolstica;

        _year = new YearField(YearEntry, YearRangeLabel);
        _year.Changed += (_, _) => ShowWhetherYearExists();
        _year.Entered += (_, _) => GoToTyped();
        _year.Show(_shownYear);

        _dateYear = new YearField(DateYearEntry, DateYearRangeLabel);
        _dateYear.Changed += (_, _) => OnDateYearChanged();
        _dateYear.Entered += (_, _) => GoToChosen();

        Picker.MinimumDate = new DateTime(1900, 1, 1);
        Picker.MaximumDate = new DateTime(9999, 12, 31);

        ShowPeriods();

        foreach (var button in new[] { YearTab, PeriodTab, DateTab, GregorianButton, SolsticaButton })
            StyleTab(button);

        _chosen = OpeningDay();
        _calendar = GregorianButton;
        SelectCalendar(GregorianButton);
        ShowChosen();

        _selected = YearTab;
        Select(YearTab);
    }

    private static SolsticaCalendar Cal => CalendarStart.Calendar;

    private void OnBackClicked(object? sender, EventArgs e) => Navigation.PopAsync();

    // ---------- the year ----------

    private void ShowWhetherYearExists()
    {
        GoButton.IsEnabled = _year.Year is not null;
        GoButton.Opacity = GoButton.IsEnabled ? 1 : 0.4;
    }

    private void OnGoClicked(object? sender, EventArgs e) => GoToTyped();

    private void GoToTyped()
    {
        if (_year.Year is { } year) Go(() => _goToYear(year));
    }

    // ---------- the periods ----------

    /// <summary>
    /// The eleven rows of the table, named by their years. A period that begins before the
    /// calendar does opens at the calendar's first year instead: the years before it are not
    /// years of this calendar at all.
    /// </summary>
    private void ShowPeriods()
    {
        var shown = SolsticaCalendar.PeriodFor(_shownYear);

        foreach (var period in ValidityPeriod.Table)
        {
            int opens = Math.Max(period.FirstYear, YearField.First);
            Periods.Add(ChoiceRow.Build(
                // Two years and a dash: nothing here to translate, and the tab above has
                // already said what they are the years of.
                $"{period.FirstYear}–{period.LastYear}",
                period.FirstYear == shown.FirstYear,
                null,
                () => Go(() => _goToYear(opens))));
        }
    }

    // ---------- the date ----------

    /// <summary>
    /// Today, when the year on screen is the one it falls in; otherwise that year's first day.
    /// Opening the tab from a year the calendar has not reached should not begin by saying so.
    /// </summary>
    private SolsticaDate OpeningDay()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        if (Cal.IsInRange(_shownYear) && today >= Cal.Epoch.AdoptionDate)
        {
            var day = Cal.FromGregorian(today);
            if (day.Year == _shownYear) return day;
        }

        return new SolsticaDate(_shownYear, PeriodKind.Unua, 1);
    }

    private void OnCalendarClicked(object? sender, EventArgs e)
    {
        if (sender is not Button side || side == _calendar) return;

        // The day survives the crossing; only the way of saying it changes. A Gregorian date
        // from before the calendar names no day at all, so it is what gets left behind.
        _beforeStart = false;
        SelectCalendar(side);
        ShowChosen();
    }

    private void SelectCalendar(Button side)
    {
        Paint(_calendar, chosen: false);
        Paint(side, chosen: true);
        _calendar = side;

        GregorianSide.IsVisible = side == GregorianButton;
        SolsticaSide.IsVisible = side == SolsticaButton;

        if (GregorianSide.IsVisible) _dateYear.LetGo();
    }

    /// <summary>Both sides, from the one day underneath them.</summary>
    private void ShowChosen()
    {
        _filling = true;

        ShowGregorianOf(_chosen);
        _dateYear.Show(_chosen.Year);
        ShowBlocks();

        _filling = false;
        ShowWhetherDateExists();
    }

    /// <summary>
    /// The Gregorian side. In the last year the Gregorian calendar, as .NET writes it, runs out
    /// before the Solstica one does, and for those days there is no date to pick: saying so is
    /// the whole of that side.
    /// </summary>
    private void ShowGregorianOf(SolsticaDate date)
    {
        bool convertible = Cal.CanConvert(date);

        if (convertible) Picker.Date = Cal.ToGregorian(date).ToDateTime(TimeOnly.MinValue);

        Picker.IsVisible = convertible;
        NoGregorianLabel.IsVisible = !convertible;
        NoGregorianLabel.Text = AppStrings.BeyondGregorian;
    }

    /// <summary>The blocks of the chosen year, and the days of the chosen block.</summary>
    private void ShowBlocks()
    {
        _blocks = SolsticaCalendar.Blocks(_chosen.Year);

        BlockPicker.ItemsSource = _blocks.Select(b => b.Kind.Name()).ToList();
        BlockPicker.SelectedIndex = IndexOf(_chosen.Period);

        int days = _blocks[BlockPicker.SelectedIndex].Days;

        // A Jarfino or a Supertago is the whole of its block; there is no day to choose.
        DayPicker.IsVisible = days > 1;
        DayPicker.ItemsSource = Enumerable.Range(1, days).Select(d => d.ToString(Language.Culture)).ToList();
        DayPicker.SelectedIndex = _chosen.Day - 1;
    }

    private int IndexOf(PeriodKind block)
    {
        for (int i = 0; i < _blocks.Count; i++)
            if (_blocks[i].Kind == block) return i;
        return 0;
    }

    private void OnGregorianPicked(object? sender, DateChangedEventArgs e)
    {
        if (_filling || e.NewDate is not { } chosen) return;

        var picked = DateOnly.FromDateTime(chosen);
        _beforeStart = picked < Cal.Epoch.AdoptionDate;

        if (!_beforeStart)
        {
            _chosen = Cal.FromGregorian(picked);

            _filling = true;
            _dateYear.Show(_chosen.Year);
            ShowBlocks();
            _filling = false;
        }

        ShowWhetherDateExists();
    }

    /// <summary>
    /// The year of the date being built. The block and the day are kept where the new year
    /// still has them — most years have the same blocks — and otherwise the year opens at its
    /// own beginning rather than at a day that is not there.
    /// </summary>
    private void OnDateYearChanged()
    {
        if (_filling) return;

        if (_dateYear.Year is not { } year)
        {
            ShowWhetherDateExists();
            return;
        }

        var blocks = SolsticaCalendar.Blocks(year);
        var kept = blocks.FirstOrDefault(b => b.Kind == _chosen.Period);

        _chosen = kept.Kind == _chosen.Period && _chosen.Day <= kept.Days
            ? new SolsticaDate(year, _chosen.Period, _chosen.Day)
            : new SolsticaDate(year, blocks[0].Kind, 1);

        _filling = true;
        ShowBlocks();
        ShowGregorianOf(_chosen);
        _filling = false;

        ShowWhetherDateExists();
    }

    private void OnBlockPicked(object? sender, EventArgs e)
    {
        if (_filling || BlockPicker.SelectedIndex < 0) return;

        var (kind, days) = _blocks[BlockPicker.SelectedIndex];
        _chosen = new SolsticaDate(_chosen.Year, kind, Math.Min(_chosen.Day, days));

        _filling = true;
        ShowBlocks();
        ShowGregorianOf(_chosen);
        _filling = false;

        ShowWhetherDateExists();
    }

    private void OnDayPicked(object? sender, EventArgs e)
    {
        if (_filling || DayPicker.SelectedIndex < 0) return;

        _chosen = new SolsticaDate(_chosen.Year, _chosen.Period, DayPicker.SelectedIndex + 1);

        _filling = true;
        ShowGregorianOf(_chosen);
        _filling = false;

        ShowWhetherDateExists();
    }

    /// <summary>
    /// Whether there is a day to go to. A year half-typed on the Solstica side is not one yet,
    /// and a Gregorian date from before the calendar begins is answered with a sentence rather
    /// than with nothing happening.
    /// </summary>
    private void ShowWhetherDateExists()
    {
        BeforeStartLabel.IsVisible = _beforeStart;
        if (_beforeStart)
            BeforeStartLabel.Text = string.Format(Language.Culture, AppStrings.BeforeCalendarBegins,
                                                  Text.LongDate(Cal.Epoch.AdoptionDate));

        bool reachable = !_beforeStart && _chosen.IsValid && Cal.IsInRange(_chosen.Year)
                         && (_calendar == GregorianButton || _dateYear.Year == _chosen.Year);

        DateGoButton.IsEnabled = reachable;
        DateGoButton.Opacity = reachable ? 1 : 0.4;
    }

    private void OnGoDateClicked(object? sender, EventArgs e) => GoToChosen();

    private void GoToChosen()
    {
        if (DateGoButton.IsEnabled) Go(() => _goToDate(_chosen));
    }

    // ---------- leaving ----------

    /// <summary>
    /// Away first, then the year. The panel is what the reader asked to leave, and the year
    /// view behind it is the thing that decides what arriving there looks like.
    /// </summary>
    private async void Go(Action arrive)
    {
        // The keyboard belongs to the fields, and the fields are about to be gone. Letting go
        // of the focus is not enough on Android: the soft input has to be dismissed itself, or
        // it stays up over a year view that has nothing to type into.
        await _year.DismissKeyboard();
        await _dateYear.DismissKeyboard();
        _year.LetGo();
        _dateYear.LetGo();

        await Navigation.PopAsync();
        arrive();
    }

    // ---------- the tabs ----------

    private void OnTabClicked(object? sender, EventArgs e)
    {
        if (sender is Button tab && tab != _selected) Select(tab);
    }

    private void Select(Button tab)
    {
        Paint(_selected, chosen: false);
        Paint(tab, chosen: true);
        _selected = tab;

        Grid.SetColumn(TabUnderline, Grid.GetColumn(tab));

        YearPanel.IsVisible = tab == YearTab;
        PeriodPanel.IsVisible = tab == PeriodTab;
        DatePanel.IsVisible = tab == DateTab;

        // A keyboard left standing over a list of periods belongs to nothing on screen.
        if (YearPanel.IsVisible) ShowWhetherYearExists();
        else _year.LetGo();

        if (DatePanel.IsVisible) ShowWhetherDateExists();
        else _dateYear.LetGo();
    }

    private static void Paint(Button button, bool chosen)
    {
        button.TextColor = (Color)Application.Current!.Resources[chosen ? "Ink" : "Muted"];
        button.FontFamily = chosen ? "PlexSemiBold" : "PlexMedium";
    }

    private static void StyleTab(Button tab)
    {
        tab.FontFamily = "PlexMedium";
        tab.FontSize = 13;
        tab.BackgroundColor = Colors.Transparent;
        tab.BorderWidth = 0;
        tab.MinimumHeightRequest = 44;
        tab.TextColor = (Color)Application.Current!.Resources["Muted"];
    }
}
