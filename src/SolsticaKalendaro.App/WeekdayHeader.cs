namespace SolsticaKalendaro.App;

/// <summary>
/// The seven column heads of the Solstica week, Monday to Sunday, with the rest days shaded and
/// red as their whole columns are. Said in one place because the year view and the
/// introduction's example have to head their columns identically — an example that explains the
/// view by being the view cannot afford to differ from it.
/// </summary>
public static class WeekdayHeader
{
    /// <param name="withRule">The hairline that closes the header off from the year below it.</param>
    public static View Build(bool withRule)
    {
        var header = new Grid
        {
            ColumnDefinitions = [.. Enumerable.Range(0, 7).Select(_ => new ColumnDefinition(GridLength.Star))],
            Padding = new Thickness(12, 6, 12, 7)
        };

        string[] names = Text.WeekdayInitials();
        var rest = RestDays.Chosen;

        for (int i = 0; i < names.Length; i++)
        {
            // A rest day is a whole column, so its head is shaded like the days under it.
            bool off = rest.Contains((DayOfWeek)(((int)DayOfWeek.Monday + i) % 7));

            if (off) header.Add(new BoxView { Color = Resource("DayOffShade") }, i);

            header.Add(new Label
            {
                Text = names[i],
                FontFamily = "PlexSemiBold",
                FontSize = 10.5,
                CharacterSpacing = 0.6,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Resource(off ? "DayOffRed" : "Muted")
            }, i);
        }

        if (withRule)
        {
            var rule = new BoxView
            {
                Color = Resource("Hairline"),
                HeightRequest = 1,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 0, -7)
            };

            header.Add(rule);
            Grid.SetColumnSpan(rule, 7);
        }

        return header;
    }

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];
}
