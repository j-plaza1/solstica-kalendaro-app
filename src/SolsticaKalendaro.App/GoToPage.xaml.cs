using SolsticaKalendaro.App.Resources.Strings;

namespace SolsticaKalendaro.App;

/// <summary>
/// The one panel behind every way of moving about: Year and Period are #15, Date is #16. What
/// this issue places is the panel and its three tabs, so that neither of those has to invent a
/// way in of its own. The tabs switch; their contents are theirs to write.
/// </summary>
public partial class GoToPage : ContentPage
{
    private Button _selected;

    public GoToPage()
    {
        InitializeComponent();

        SemanticProperties.SetHint(BackButton, AppStrings.HintBackToYear);
        TitleLabel.Text = AppStrings.GoTo;
        YearTab.Text = AppStrings.TabYear;
        PeriodTab.Text = AppStrings.TabPeriod;
        DateTab.Text = AppStrings.TabDate;

        foreach (var tab in new[] { YearTab, PeriodTab, DateTab }) StyleTab(tab);
        _selected = YearTab;
        Select(YearTab);
    }

    private void OnBackClicked(object? sender, EventArgs e) => Navigation.PopAsync();

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
