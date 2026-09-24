using Microsoft.Maui.Layouts;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>
/// One day, in full. Every number and date here comes from <see cref="SolsticaCalendar.Describe"/>
/// and <see cref="SolsticaCalendar.UpcomingFestivities"/>; the Catalan around them is the app's,
/// because the core states facts and does not write sentences.
/// </summary>
public partial class DayDetailPage : ContentPage
{
    private const int FestivitiesShown = 3;

    private readonly SolsticaCalendar _calendar;

    public DayDetailPage(SolsticaCalendar calendar, SolsticaDate date)
    {
        InitializeComponent();
        _calendar = calendar;

        var detail = calendar.Describe(date);
        var palette = Palette();

        BackButton.Text = $"‹   {detail.Date.Year}";
        PositionLabel.Text = $"dia {detail.DayOfYear} de {detail.DaysInYear}";

        // A festivity that has arrived is no longer upcoming, so the day announces its own.
        if (detail.Date.FestivityName is { } festivity)
        {
            FestivityLabel.Text = festivity;
            FestivityLabel.IsVisible = true;
        }

        WeekDayLabel.Text = detail.WeekDay is { } weekDay
            ? Text.WeekDay(weekDay)
            : "fora del cicle setmanal";
        WeekDayLabel.TextColor = palette[detail.Season];

        DateLabel.Text = Text.DayName(detail.Date);
        DateYearLabel.Text = detail.Date.Year.ToString(YearView.Culture);

        GregorianLabel.Text = detail.Gregorian is { } gregorian
            ? Text.FullDate(gregorian)
            : "fora de l'abast del calendari gregorià";

        ShowDivergence(detail);
        ShowSeason(detail, palette);
        ShowFestivities(detail);

        FooterLabel.Text =
            $"Període {detail.Period.FirstYear}–{detail.Period.LastYear} · {detail.Period.Allocation}";
    }

    private void OnBackClicked(object? sender, EventArgs e) => Navigation.PopAsync();

    /// <summary>
    /// Why the two weekdays disagree, said only where they do. An extra-weekly day is both the
    /// cause and the exception: it has no weekday of its own to disagree with.
    /// </summary>
    private void ShowDivergence(DayDetail detail)
    {
        if (detail.Date.Period.IsExtraWeekly())
        {
            DivergenceTitle.Text = "Per què aquest dia és diferent";
            DivergenceBody.Text = "Aquest dia no té dia de la setmana: és el que endarrereix el compte.";
            DivergenceCard.IsVisible = true;
            return;
        }

        if (detail.WeekDay is not { } solstica || detail.GregorianWeekDay is not { } gregorian) return;

        int behind = ((int)gregorian - (int)solstica + 7) % 7;
        if (behind == 0) return;

        DivergenceTitle.Text = "Per què els dies de la setmana no coincideixen";
        DivergenceBody.Text =
            "Cada dia extra-setmanal atura un dia el compte setmanal, i per això el calendari "
            + $"solstici va {Text.Days(behind)} per darrere del gregorià. "
            + $"L'últim va ser {Text.Shift(detail.LastShift)}; {NextShiftClause(detail.NextShift)}";

        DivergenceCard.IsVisible = true;
    }

    private string NextShiftClause(SolsticaDate? next)
    {
        if (next is not { } shift) return "i no n'hi ha cap més en aquest calendari.";

        string when = _calendar.CanConvert(shift) ? Text.LongDate(_calendar.ToGregorian(shift)) : string.Empty;
        return when.Length > 0
            ? $"el pròxim serà {Text.Shift(shift)}, el {when}."
            : $"el pròxim serà {Text.Shift(shift)}.";
    }

    /// <summary>
    /// The seasonal bar: four segments in proportion to the real lengths of this year's seasons,
    /// the day marked where it falls, and the cardinal points set at the boundaries they actually
    /// land on rather than spaced evenly across the bar.
    /// </summary>
    private void ShowSeason(DayDetail detail, YearPalette palette)
    {
        SeasonLabel.Text =
            $"{Text.SeasonName(detail.Season)} · dia {detail.DayOfSeason} de {detail.SeasonLength}";

        var seasons = Enum.GetValues<Season>();
        double total = detail.DaysInYear;
        double cumulative = 0;

        for (int i = 0; i < seasons.Length; i++)
        {
            int length = detail.Period.SeasonLength(seasons[i], detail.Date.Year);

            SeasonBar.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(length, GridUnitType.Star)));
            var segment = new BoxView { Color = palette[seasons[i]] };
            Grid.SetColumn(segment, i);
            SeasonBar.Children.Add(segment);

            AddDegreeLabel(seasons[i].OpeningLongitude(), cumulative / total);
            cumulative += length;
        }

        // The year closes where it opened, back at the December solstice.
        AddDegreeLabel(Season.First.OpeningLongitude(), 1);

        AbsoluteLayout.SetLayoutBounds(Marker, new Rect((detail.DayOfYear - 1) / total, 0, 3, 18));
    }

    private void AddDegreeLabel(int degrees, double fraction)
    {
        var label = new Label
        {
            Text = $"{degrees}°",
            FontFamily = "Plex",
            FontSize = 9.5,
            TextColor = Resource("Faint")
        };

        AbsoluteLayout.SetLayoutFlags(label, AbsoluteLayoutFlags.XProportional);
        AbsoluteLayout.SetLayoutBounds(label, new Rect(fraction, 0, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        DegreeRow.Children.Add(label);
    }

    private void ShowFestivities(DayDetail detail)
    {
        foreach (var festivity in _calendar.UpcomingFestivities(detail.Date, FestivitiesShown))
        {
            var row = new Grid
            {
                ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
                Padding = new Thickness(0, 11),
                ColumnSpacing = 12
            };

            row.Add(new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label
                    {
                        Text = festivity.Name,
                        FontFamily = "SpectralSemiBold",
                        FontSize = 16,
                        TextColor = Resource("Ink")
                    },
                    new Label
                    {
                        Text = Text.FestivityWhen(festivity, detail.Date.Year),
                        FontFamily = "Plex",
                        FontSize = 10.5,
                        TextColor = Resource("Muted")
                    }
                }
            });

            row.Add(new Label
            {
                Text = Text.Away(festivity.DaysAway),
                FontFamily = "Plex",
                FontSize = 11.5,
                TextColor = Resource("Ink"),
                VerticalOptions = LayoutOptions.Center
            }, 1);

            Festivities.Add(new BoxView { Color = Resource("Hairline"), HeightRequest = 1 });
            Festivities.Add(row);
        }
    }

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];

    private static YearPalette Palette() => new(
        Resource("SeasonFirst"), Resource("SeasonSecond"), Resource("SeasonThird"), Resource("SeasonFourth"));
}
