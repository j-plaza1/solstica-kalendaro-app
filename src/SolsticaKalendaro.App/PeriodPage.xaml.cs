using SolsticaKalendaro.App.Resources.Strings;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>
/// One row of the table of section 9.3, said in sentences: how the year's days are shared among
/// the seasons, how wide the transition blocks are, and — for every period but the first — what
/// changed when it began. Someone who reaches 3324 and finds a Jarmezo of seven days is looking
/// at the calendar's one structural reform, and this is where that is explained rather than
/// left to look like a fault.
///
/// Every number and every name here comes from the core. The page writes the sentences and
/// decides nothing else; the error columns of the table stay in the document, whose vocabulary
/// they are.
/// </summary>
public partial class PeriodPage : ContentPage
{
    private ValidityPeriod _period;

    public PeriodPage(ValidityPeriod period)
    {
        _period = period;

        InitializeComponent();

        SemanticProperties.SetHint(BackButton, AppStrings.HintBack);
        SemanticProperties.SetHint(PreviousButton, AppStrings.HintPreviousPeriod);
        SemanticProperties.SetHint(NextButton, AppStrings.HintNextPeriod);

        KindLabel.Text = AppStrings.TabPeriod.ToUpper(Language.Culture);
        SeasonDaysHeading.Text = AppStrings.SeasonDays.ToUpper(Language.Culture);
        BlocksHeading.Text = AppStrings.TransitionBlocks.ToUpper(Language.Culture);

        ArticleLabel.Text = string.Format(Language.Culture, AppStrings.ArticleVersion,
                                          SolsticaCalendar.SpecificationVersion);
        ArticleButton.Text = AppStrings.ReadArticle;

        foreach (var arrow in new[] { PreviousButton, NextButton }) StyleArrow(arrow);

        Show();
    }

    private void OnBackClicked(object? sender, EventArgs e) => Navigation.PopAsync();

    /// <summary>
    /// The arrows walk the table here, on this page. Pushing a page for each would build a
    /// stack eleven deep to read a table, and moving the year underneath would answer a
    /// question the reader has not asked.
    /// </summary>
    private void OnPrevious(object? sender, EventArgs e) => Show(_period.Previous);

    private void OnNext(object? sender, EventArgs e) => Show(_period.Next);

    private void Show(ValidityPeriod? period = null)
    {
        _period = period ?? _period;

        // Years are written as digits, never grouped: 10000, not 10,000.
        YearsLabel.Text = $"{_period.FirstYear}–{_period.LastYear}";

        PreviousButton.IsEnabled = _period.Previous is not null;
        NextButton.IsEnabled = _period.Next is not null;
        PreviousButton.Opacity = PreviousButton.IsEnabled ? 1 : 0.3;
        NextButton.Opacity = NextButton.IsEnabled ? 1 : 0.3;

        ShowSeasons();
        ShowBlocks();
        ShowChanges();

        Scroller.ScrollToAsync(0, 0, animated: false);
    }

    // ---------- the year's shape ----------

    private void ShowSeasons()
    {
        Seasons.Clear();

        foreach (var season in Enum.GetValues<Season>())
            Seasons.Add(Row(Text.SeasonTitle(season), Days(_period.Allocation[season]),
                            Resource($"Season{season}")));

        // Which season is given the extra day, and the day it comes after: a leap year is the
        // only place the period's own seam is visible.
        var supertago = _period.SupertagoSeason;
        LeapLabel.Text = string.Format(Language.Culture, AppStrings.LeapSeason,
            Text.SeasonName(supertago),
            Days(_period.SeasonLength(supertago, LeapYearOf(_period))),
            Text.BlockDay(_period.SupertagoFollows));
    }

    /// <summary>A leap year of this period, for a length that only leap years have.</summary>
    private static int LeapYearOf(ValidityPeriod period)
    {
        int year = period.FirstYear;
        while (!SolsticaCalendar.IsLeapYear(year)) year++;
        return year;
    }

    private void ShowBlocks()
    {
        Blocks.Clear();

        foreach (var block in new[] { PeriodKind.EkvinoksoI, PeriodKind.Jarmezo, PeriodKind.EkvinoksoII })
            Blocks.Add(Row(block.Name(), Days(_period.Blocks.Width(block)), null));
    }

    // ---------- what changed ----------

    private void ShowChanges()
    {
        Changes.Clear();

        if (_period.ChangesOnEntering is not { } changes)
        {
            ChangesHeading.IsVisible = false;
            Changes.IsVisible = false;
            return;
        }

        ChangesHeading.IsVisible = true;
        Changes.IsVisible = true;
        ChangesHeading.Text = string.Format(Language.Culture, AppStrings.ChangesOnEntering,
                                            _period.FirstYear).ToUpper(Language.Culture);

        // Widest first: a block that changes width is what moves the months, and the months
        // are what a reader notices. The seasons are a change of meaning, and the Supertago
        // the one day it puts somewhere else.
        foreach (var (block, from, to) in changes.Blocks)
            Changes.Add(Sentence(string.Format(Language.Culture, AppStrings.BlockChanged,
                                               block.Name(), from, to)));

        if (changes.MonthsMoved.Count > 0)
            Changes.Add(Sentence(string.Format(Language.Culture, AppStrings.MonthsEarlier,
                Text.List([.. changes.MonthsMoved.Select(m => m.Month.Name())]),
                Math.Abs(changes.MonthsMoved[0].Days))));

        foreach (var (season, from, to) in changes.Seasons)
            Changes.Add(Sentence(string.Format(Language.Culture, AppStrings.SeasonChanged,
                                               Text.SeasonName(season), from, to)));

        if (changes.SupertagoFrom is { } was && changes.SupertagoTo is { } now)
            Changes.Add(Sentence(string.Format(Language.Culture, AppStrings.SupertagoMoved,
                                               Text.BlockDay(was), Text.BlockDay(now))));
    }

    // ---------- the article ----------

    private void OnReadArticle(object? sender, EventArgs e) =>
        Launcher.Default.OpenAsync(new Uri(SolsticaCalendar.SpecificationUrl));

    // ---------- the shapes on screen ----------

    private string Days(int days) => string.Format(Language.Culture, AppStrings.DaysCount, days);

    /// <summary>
    /// A name on the left and a count on the right, over a hairline, as the lists of Options
    /// are. The stripe is the season's colour; the blocks have none, being of no one season.
    /// </summary>
    private static View Row(string name, string count, Color? stripe)
    {
        var row = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(4), new ColumnDefinition(GridLength.Star),
                                 new ColumnDefinition(GridLength.Auto)],
            ColumnSpacing = 12,
            Padding = new Thickness(0, 13),
            MinimumHeightRequest = 44
        };

        if (stripe is { } colour)
            row.Add(new BoxView
            {
                Color = colour,
                CornerRadius = 2,
                HeightRequest = 20,
                VerticalOptions = LayoutOptions.Center
            });

        row.Add(new Label
        {
            Text = name,
            FontFamily = "Plex",
            FontSize = 16,
            TextColor = Resource("Ink"),
            VerticalOptions = LayoutOptions.Center
        }, 1);

        row.Add(new Label
        {
            Text = count,
            FontFamily = "Plex",
            FontSize = 15,
            TextColor = Resource("Muted"),
            VerticalOptions = LayoutOptions.Center
        }, 2);

        var holder = new VerticalStackLayout { Spacing = 0 };
        holder.Add(new BoxView { Color = Resource("Hairline"), HeightRequest = 1 });
        holder.Add(row);
        return holder;
    }

    private static View Sentence(string text) => new Label
    {
        Text = text,
        FontFamily = "Plex",
        FontSize = 13.5,
        LineHeight = 1.35,
        TextColor = Resource("Ink"),
        Margin = new Thickness(0, 10, 0, 0)
    };

    private static void StyleArrow(Button arrow)
    {
        arrow.FontFamily = "Plex";
        arrow.FontSize = 22;
        arrow.TextColor = Resource("Muted");
        arrow.BackgroundColor = Colors.Transparent;
        arrow.BorderWidth = 0;
        arrow.Padding = new Thickness(0);
        arrow.MinimumHeightRequest = 44;
        arrow.WidthRequest = 38;
    }

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];
}
