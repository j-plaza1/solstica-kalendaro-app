using SolsticaKalendaro.App.Resources.Strings;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>
/// What the reader can decide: the language, and when the calendar begins. #20 adds the rest
/// days, which is why this is a page rather than a dialog.
///
/// Both choices rebuild the pages, because everything already drawn was drawn with the old
/// answer. Both keep the reader where they were: on this page, scrolled to where they were
/// reading it, and behind it the year they were reading, at the row they had reached.
/// </summary>
public partial class OptionsPage : ContentPage
{
    private readonly double _resumeScroll;

    /// <param name="resumeScroll">
    /// How far down the reader had come, when this page is replacing one they were already
    /// looking at. The lists are longer than a phone screen, so the tick that has just moved
    /// may well be below the fold, and a page that opened at the top would hide the answer to
    /// what the reader just did.
    /// </param>
    public OptionsPage(double resumeScroll = 0)
    {
        InitializeComponent();
        _resumeScroll = resumeScroll;

        SemanticProperties.SetHint(BackButton, AppStrings.HintBack);
        TitleLabel.Text = AppStrings.MenuOptions;
        LanguageHeading.Text = AppStrings.Language.ToUpper(Language.Culture);
        CalendarBeginsHeading.Text = AppStrings.CalendarBegins.ToUpper(Language.Culture);
        CalendarBeginsNote.Text = AppStrings.CalendarBeginsNote;

        ShowLanguages();
        ShowStarts();

        // Loaded comes too early: the page exists but nothing has been measured, and a scroll
        // to a position the content does not yet reach is clamped to the top. The first size
        // the ScrollView is given is the moment there is something to scroll within.
        if (_resumeScroll > 0) Scroller.SizeChanged += RestoreScrollOnce;
    }

    /// <summary>
    /// Restored once. Detached at the first attempt, so that every later layout — a rotation,
    /// a keyboard — leaves the scrolling to the reader, whose it is by then.
    /// </summary>
    private void RestoreScrollOnce(object? sender, EventArgs e)
    {
        if (Scroller.Height <= 0) return;

        Scroller.SizeChanged -= RestoreScrollOnce;
        Scroller.ScrollToAsync(0, _resumeScroll, animated: false);
    }

    private void OnBackClicked(object? sender, EventArgs e) => Navigation.PopAsync();

    private void ShowLanguages()
    {
        // Following the device comes first: it is what the app does until asked otherwise.
        foreach (string code in new[] { Language.Automatic }.Concat(Language.Codes))
            Languages.Add(Row(Language.NameOf(code), Language.Chosen == code, null,
                              () => ChooseLanguage(code)));
    }

    private void ShowStarts()
    {
        foreach (var epoch in CalendarStart.Options)
            Starts.Add(Row(
                Text.LongDate(epoch.AdoptionDate),
                CalendarStart.Chosen.FirstSolsticaYear == epoch.FirstSolsticaYear,
                // Only one of them starts on a different day, and that is worth saying where
                // the reader is choosing rather than afterwards when every date has moved.
                epoch.IsOffAnchor ? AppStrings.SolsticeOn22 : null,
                () => ChooseStart(epoch)));
    }

    private View Row(string text, bool chosen, string? note, Action choose)
    {
        var name = new Label
        {
            Text = text,
            FontFamily = chosen ? "PlexSemiBold" : "Plex",
            FontSize = 16,
            TextColor = Resource(chosen ? "Ink" : "Muted"),
            VerticalOptions = LayoutOptions.Center
        };

        var lines = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        lines.Add(name);

        if (note is not null)
            lines.Add(new Label
            {
                Text = note,
                FontFamily = "Plex",
                FontSize = 11,
                TextColor = Resource("Muted")
            });

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
        row.Add(lines);
        row.Add(tick, 1);
        row.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(choose) });

        var holder = new VerticalStackLayout { Spacing = 0 };
        holder.Add(new BoxView { Color = Resource("Hairline"), HeightRequest = 1 });
        holder.Add(row);
        return holder;
    }

    private void ChooseLanguage(string code)
    {
        if (Language.Chosen == code) return;
        Language.Choose(code);
        Rebuild();
    }

    private void ChooseStart(SolsticaEpoch epoch)
    {
        if (CalendarStart.Chosen.FirstSolsticaYear == epoch.FirstSolsticaYear) return;
        CalendarStart.Choose(epoch);
        Rebuild();
    }

    /// <summary>
    /// Everything on screen was built with the old answer, so it is all built again — and both
    /// pages are told where they were: this one so the reader can see the tick that has just
    /// moved, the year underneath so that coming back is not a return to the top.
    /// </summary>
    private void Rebuild()
    {
        var place = Navigation.NavigationStack.OfType<MainPage>().FirstOrDefault()?.Place;

        var navigation = new NavigationPage(new MainPage(place));
        Application.Current!.Windows[0].Page = navigation;
        navigation.PushAsync(new OptionsPage(Scroller.ScrollY), animated: false);
    }

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];
}
