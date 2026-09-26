using SolsticaKalendaro.App.Resources.Strings;

namespace SolsticaKalendaro.App;

/// <summary>
/// What the reader can decide. For now the language; #14 adds when the calendar begins and #20
/// the rest days, which is why this is a page rather than a dialog.
/// </summary>
public partial class OptionsPage : ContentPage
{
    public OptionsPage()
    {
        InitializeComponent();

        SemanticProperties.SetHint(BackButton, AppStrings.HintBack);
        TitleLabel.Text = AppStrings.MenuOptions;
        LanguageHeading.Text = AppStrings.Language.ToUpper(Language.Culture);

        ShowLanguages();
    }

    private void OnBackClicked(object? sender, EventArgs e) => Navigation.PopAsync();

    private void ShowLanguages()
    {
        // Following the device comes first: it is what the app does until asked otherwise.
        foreach (string code in new[] { Language.Automatic }.Concat(Language.Codes))
            Languages.Add(Row(code));
    }

    private View Row(string code)
    {
        bool chosen = Language.Chosen == code;

        var name = new Label
        {
            Text = Language.NameOf(code),
            FontFamily = chosen ? "PlexSemiBold" : "Plex",
            FontSize = 16,
            TextColor = Resource(chosen ? "Ink" : "Muted"),
            VerticalOptions = LayoutOptions.Center
        };

        var tick = new Label
        {
            Text = chosen ? "✓" : string.Empty,
            FontFamily = "Plex",
            FontSize = 16,
            TextColor = Resource("Accent"),
            VerticalOptions = LayoutOptions.Center
        };

        var row = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
            Padding = new Thickness(0, 14),
            MinimumHeightRequest = 44
        };
        row.Add(name);
        row.Add(tick, 1);
        row.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Choose(code)) });

        var holder = new VerticalStackLayout { Spacing = 0 };
        holder.Add(new BoxView { Color = Resource("Hairline"), HeightRequest = 1 });
        holder.Add(row);
        return holder;
    }

    /// <summary>
    /// The app changes language where it stands. Everything already on screen was built with
    /// the old words, so the pages are built again — the reader stays here, on this page, now
    /// reading it in the language they just chose.
    /// </summary>
    private static void Choose(string code)
    {
        if (Language.Chosen == code) return;
        Language.Choose(code);

        var navigation = new NavigationPage(new MainPage());
        Application.Current!.Windows[0].Page = navigation;
        navigation.PushAsync(new OptionsPage(), animated: false);
    }

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];
}
