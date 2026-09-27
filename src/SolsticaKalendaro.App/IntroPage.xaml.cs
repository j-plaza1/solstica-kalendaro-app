using SolsticaKalendaro.App.Resources.Strings;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>
/// The way in, for a reader who has not read the proposal and should not have to: what the
/// calendar is, how to read the year, and what the names mean. Three screens, shown once on the
/// first launch and available afterwards from the menu and from About.
///
/// The middle screen shows a real week and a real Jarfino, drawn with the year view's own row
/// templates, so that what is explained looks exactly like what the reader meets next.
/// </summary>
public partial class IntroPage : ContentPage
{
    private const int Screens_ = 3;

    private readonly bool _firstTime;

    /// <param name="firstTime">
    /// True on the first launch, where the last button starts the app and skipping is offered.
    /// False when the introduction was asked for, where the last button simply closes it.
    /// </param>
    public IntroPage(bool firstTime)
    {
        _firstTime = firstTime;

        InitializeComponent();

        Screens.ItemsSource = new List<View> { WhatScreen(), ReadScreen(), NamesScreen() };
        ShowButtons();
    }

    private static SolsticaCalendar Cal => CalendarStart.Calendar;

    // ---------- moving, and leaving ----------

    private void OnScreenChanged(object? sender, PositionChangedEventArgs e) => ShowButtons();

    /// <summary>
    /// Skip is offered while there is something left to skip, whichever way the introduction
    /// was opened: a reader who came looking for it can still decide they have seen enough.
    /// The last button says what it does — start the app, or close what was asked for.
    /// </summary>
    private void ShowButtons()
    {
        bool last = Screens.Position >= Screens_ - 1;

        SkipButton.Text = AppStrings.IntroSkip;
        SkipButton.IsVisible = !last;

        NextButton.Text = last
            ? _firstTime ? AppStrings.IntroStart : AppStrings.IntroClose
            : AppStrings.IntroNext;
    }

    private void OnSkip(object? sender, EventArgs e) => Close();

    private void OnNext(object? sender, EventArgs e)
    {
        if (Screens.Position < Screens_ - 1) Screens.Position++;
        else Close();
    }

    /// <summary>
    /// Android's own back button, which closes a modal page by itself. Seen is seen however the
    /// reader leaves: skipping and finishing say the same thing about wanting it again.
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        Introduction.MarkSeen();
        return base.OnBackButtonPressed();
    }

    private void Close()
    {
        Introduction.MarkSeen();
        Navigation.PopModalAsync(animated: true);
    }

    // ---------- what the calendar is ----------

    private static View WhatScreen()
    {
        var screen = Screen(AppStrings.IntroTitle1);
        screen.Add(Paragraph(AppStrings.IntroWhat1));
        screen.Add(Paragraph(AppStrings.IntroWhat2));

        // Where to change it is a place in this app, so it is named as this app names it.
        screen.Add(Paragraph(string.Format(Language.Culture, AppStrings.IntroWhat3,
                                           AppStrings.MenuOptions)));
        return Scrollable(screen);
    }

    // ---------- how to read the year ----------

    private static View ReadScreen()
    {
        var screen = Screen(AppStrings.MenuHowToRead);
        screen.Add(Example());
        screen.Add(Paragraph(AppStrings.IntroRead1));
        screen.Add(Paragraph(AppStrings.IntroRead2));
        screen.Add(Paragraph(AppStrings.IntroRead3));
        screen.Add(Paragraph(AppStrings.IntroRead4));
        return Scrollable(screen);
    }

    /// <summary>
    /// The last week of a year and the day that closes it, from the calendar the reader has
    /// actually got, drawn with the year view's own templates. Nothing is today and nothing is
    /// marked: it is an example, and it explains the view by being the view.
    /// </summary>
    private static View Example()
    {
        var outline = Cal.Outline(Cal.Epoch.FirstSolsticaYear);
        var palette = Palette();

        // Outside any year the app can show, so no cell claims to be today.
        var rows = YearView.Build(outline, DateOnly.MinValue, palette);
        var shown = new List<RowView> { rows[^2], rows[^1] };

        var days = new VerticalStackLayout { Spacing = 0 };
        BindableLayout.SetItemTemplateSelector(days, Selector());
        BindableLayout.SetItemsSource(days, shown);

        var example = new VerticalStackLayout
        {
            Spacing = 0,
            Margin = new Thickness(0, 4, 0, 18),
            InputTransparent = true,
            CascadeInputTransparent = true
        };
        example.Add(WeekdayHeader.Build(withRule: false));
        example.Add(days);
        return example;
    }

    // ---------- what the names mean ----------

    private static View NamesScreen()
    {
        var screen = Screen(AppStrings.IntroTitle3);
        screen.Add(Paragraph(AppStrings.IntroNamesLead));

        // The names are the calendar's own and are never translated; only what they mean is.
        string months = string.Join(", ",
            new[] { PeriodKind.Unua, PeriodKind.Dua, PeriodKind.Tria }.Select(p => p.Name()));

        screen.Add(Meaning($"{months}… {PeriodKind.DekDua.Name()}", AppStrings.IntroNameMonths));
        screen.Add(Meaning(string.Format(Language.Culture, AppStrings.ListAnd,
                                         PeriodKind.EkvinoksoI.Name(), "II"),
                           AppStrings.IntroNameEkvinokso));
        screen.Add(Meaning(PeriodKind.Jarmezo.Name(), AppStrings.IntroNameJarmezo));
        screen.Add(Meaning("Jarkomenco", AppStrings.IntroNameJarkomenco));
        screen.Add(Meaning("Rekomenco", AppStrings.IntroNameRekomenco));
        screen.Add(Meaning(PeriodKind.Jarfino.Name(), AppStrings.IntroNameJarfino));
        screen.Add(Meaning(PeriodKind.Supertago.Name(), AppStrings.IntroNameSupertago));

        return Scrollable(screen);
    }

    private static View Meaning(string name, string meaning) => new Label
    {
        FormattedText = new FormattedString
        {
            Spans =
            {
                new Span { Text = name, FontFamily = "PlexSemiBold" },
                new Span { Text = $": {meaning}", FontFamily = "Plex" }
            }
        },
        FontSize = 14,
        LineHeight = 1.4,
        TextColor = Resource("Ink"),
        Margin = new Thickness(0, 0, 0, 10)
    };

    // ---------- the shape of a screen ----------

    private static VerticalStackLayout Screen(string title)
    {
        var screen = new VerticalStackLayout { Spacing = 0 };
        screen.Add(new Label
        {
            Text = title,
            FontFamily = "SpectralSemiBold",
            FontSize = 27,
            TextColor = Resource("Ink"),
            Margin = new Thickness(0, 0, 0, 16)
        });
        return screen;
    }

    private static View Scrollable(View content) => new ScrollView
    {
        Content = content,
        Padding = new Thickness(20, 28, 20, 10)
    };

    private static View Paragraph(string text) => new Label
    {
        Text = text,
        FontFamily = "Plex",
        FontSize = 14,
        LineHeight = 1.45,
        TextColor = Resource("Ink"),
        Margin = new Thickness(0, 0, 0, 14)
    };

    private static DataTemplateSelector Selector() =>
        (DataTemplateSelector)Application.Current!.Resources["RowSelector"];

    private static YearPalette Palette() => new(
        Resource("SeasonFirst"), Resource("SeasonSecond"),
        Resource("SeasonThird"), Resource("SeasonFourth"));

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];
}
