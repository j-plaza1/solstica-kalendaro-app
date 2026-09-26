using System.Globalization;
using SolsticaKalendaro.App.Resources.Strings;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>
/// The one panel behind every way of moving about: a year typed, a period chosen, and a
/// Gregorian date to come (#16). All three answer the same question and land the same way — at
/// the top of a year, with the panel closed behind them.
///
/// The panel knows which year is on screen and how to ask for another; it holds no calendar of
/// its own and decides nothing about what the year view then does.
/// </summary>
public partial class GoToPage : ContentPage
{
    private readonly int _shownYear;
    private readonly Action<int> _goToYear;
    private Button _selected;

    /// <param name="shownYear">The year the reader came from: what the field holds, and which
    /// period is ticked.</param>
    /// <param name="goToYear">How to ask the year view for a year, once this page is gone.</param>
    public GoToPage(int shownYear, Action<int> goToYear)
    {
        _shownYear = shownYear;
        _goToYear = goToYear;

        InitializeComponent();

        SemanticProperties.SetHint(BackButton, AppStrings.HintBackToYear);
        TitleLabel.Text = AppStrings.GoTo;
        YearTab.Text = AppStrings.TabYear;
        PeriodTab.Text = AppStrings.TabPeriod;
        DateTab.Text = AppStrings.TabDate;

        GoButton.Text = AppStrings.GoButton;
        YearRangeLabel.Text = string.Format(Language.Culture, AppStrings.YearRange, FirstYear, LastYear);
        YearEntry.Text = _shownYear.ToString(Language.Culture);

        ShowPeriods();

        foreach (var tab in new[] { YearTab, PeriodTab, DateTab }) StyleTab(tab);
        _selected = YearTab;
        Select(YearTab);
    }

    /// <summary>The years there are: the calendar's own first, and the last the table covers.</summary>
    private static int FirstYear => CalendarStart.Chosen.FirstSolsticaYear;
    private static int LastYear => ValidityPeriod.Table[^1].LastYear;

    private void OnBackClicked(object? sender, EventArgs e) => Navigation.PopAsync();

    // ---------- the year ----------

    /// <summary>
    /// The year typed, if it is one the calendar has. Parsed as plain digits: the numeric
    /// keyboard gives nothing else, and a grouping separator or a sign would not be a year.
    /// </summary>
    private int? TypedYear =>
        int.TryParse(YearEntry.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int year)
        && year >= FirstYear && year <= LastYear
            ? year
            : null;

    /// <summary>
    /// All of it selected, so that typing replaces the year rather than appending to it. Android
    /// moves the caret itself as the field takes focus, so this has to come after that.
    /// </summary>
    private void OnYearFocused(object? sender, FocusEventArgs e) => Dispatcher.Dispatch(() =>
    {
        YearEntry.CursorPosition = 0;
        YearEntry.SelectionLength = YearEntry.Text?.Length ?? 0;
    });

    private void OnYearChanged(object? sender, TextChangedEventArgs e) => ShowWhetherYearExists();

    private void ShowWhetherYearExists()
    {
        GoButton.IsEnabled = TypedYear is not null;
        GoButton.Opacity = GoButton.IsEnabled ? 1 : 0.4;
    }

    /// <summary>The keyboard's own key, which means the same as the button beside it.</summary>
    private void OnYearCompleted(object? sender, EventArgs e) => GoToTyped();

    private void OnGoClicked(object? sender, EventArgs e) => GoToTyped();

    private void GoToTyped()
    {
        if (TypedYear is { } year) Go(year);
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
            int opens = Math.Max(period.FirstYear, FirstYear);
            Periods.Add(ChoiceRow.Build(
                // Two years and a dash: nothing here to translate, and the tab above has
                // already said what they are the years of.
                $"{period.FirstYear}–{period.LastYear}",
                period.FirstYear == shown.FirstYear,
                null,
                () => Go(opens)));
        }
    }

    // ---------- leaving ----------

    /// <summary>
    /// Away first, then the year. The panel is what the reader asked to leave, and the year
    /// view behind it is the thing that decides what arriving at a year looks like.
    /// </summary>
    private async void Go(int year)
    {
        // The keyboard belongs to the field, and the field is about to be gone. Letting go of
        // the focus is not enough on Android: the soft input has to be dismissed itself, or it
        // stays up over a year view that has nothing to type into.
        await YearEntry.HideSoftInputAsync(CancellationToken.None);
        YearEntry.Unfocus();

        await Navigation.PopAsync();
        _goToYear(year);
    }

    // ---------- the tabs ----------

    private void OnTabClicked(object? sender, EventArgs e)
    {
        if (sender is Button tab && tab != _selected) Select(tab);
    }

    private void Select(Button tab)
    {
        _selected.TextColor = (Color)Application.Current!.Resources["Muted"];
        _selected.FontFamily = "PlexMedium";

        tab.TextColor = (Color)Application.Current!.Resources["Ink"];
        tab.FontFamily = "PlexSemiBold";
        _selected = tab;

        Grid.SetColumn(TabUnderline, Grid.GetColumn(tab));

        YearPanel.IsVisible = tab == YearTab;
        PeriodPanel.IsVisible = tab == PeriodTab;
        DatePanel.IsVisible = tab == DateTab;

        // A keyboard left standing over a list of periods belongs to nothing on screen.
        if (!YearPanel.IsVisible) YearEntry.Unfocus();
        else ShowWhetherYearExists();
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
