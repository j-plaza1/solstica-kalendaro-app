using SolsticaKalendaro.App.Resources.Strings;

namespace SolsticaKalendaro.App;

/// <summary>
/// A page that exists so that the structure can be walked before its screens are written. It
/// carries its title and nothing else. One is left: Com es llegeix, which #19 replaces. Options
/// came with #13 and #14, the period with #17 and About with #18.
/// </summary>
public partial class PlaceholderPage : ContentPage
{
    public PlaceholderPage(string title)
    {
        InitializeComponent();
        SemanticProperties.SetHint(BackButton, AppStrings.HintBack);
        TitleLabel.Text = title;
    }

    private void OnBackClicked(object? sender, EventArgs e) => Navigation.PopAsync();
}
