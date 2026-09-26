using SolsticaKalendaro.App.Resources.Strings;

namespace SolsticaKalendaro.App;

/// <summary>
/// A page that exists so that the structure can be walked before its screens are written. It
/// carries its title and nothing else. Each one is replaced by the issue that fills it:
/// Opcions by #13, #14 and #20; Com es llegeix by #19; Quant a by #18; Període by #17.
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
