namespace SolsticaKalendaro.App;

/// <summary>
/// One line of a list to choose from: the languages and start dates of Options, the periods of
/// the Go to panel. A hairline above, the chosen one in ink with a tick beside it and the rest
/// muted, so that a reader who has read one such list can read the next without looking twice.
/// </summary>
public static class ChoiceRow
{
    /// <param name="note">A second line under the name, for the one thing worth saying about
    /// that choice. Most rows have nothing to add and pass null.</param>
    public static View Build(string text, bool chosen, string? note, Action choose)
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

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];
}
