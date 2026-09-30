namespace SolsticaKalendaro.App;

/// <summary>
/// Every colour the app uses, as a light/dark pair. The app follows the system's theme and has
/// no setting of its own, as with the text size: it is what the reader has already chosen, for
/// every app alike.
///
/// The pairs are chosen together rather than one at a time — the seasonal stripe is read by
/// comparing four colours with each other — and the dark values meet 4.5:1 for text and 3:1 for
/// the stripes against the dark paper and the day-off shade.
///
/// The active value of each is written into the application's resources under the name the rest
/// of the app already used, so nothing else has to know which theme is in force.
/// </summary>
public static class Palette
{
    private static readonly (string Key, string Light, string Dark)[] Colours =
    [
        ("Paper",        "#FAF7F0", "#1A1815"),
        ("Ink",          "#1C1A17", "#ECE6DA"),
        ("Muted",        "#6B6459", "#A89F90"),

        // Darkened from #837A6C: the small Gregorian dates reached only 3.95:1 on the paper
        // and 3.4:1 on the day-off shade, and they are the smallest text in the app.
        ("Faint",        "#6E665A", "#9B9285"),

        ("Hairline",     "#E3DCCE", "#33302B"),
        ("CellEdge",     "#EDE7DA", "#2A2723"),
        ("Rule",         "#C9C0B0", "#4A453D"),
        ("Accent",       "#A8543C", "#D98A6E"),

        ("SeasonFirst",  "#4A6FA5", "#6F95CF"),
        ("SeasonSecond", "#4F7A4A", "#7DAA73"),
        ("SeasonThird",  "#C1762E", "#E0995A"),
        ("SeasonFourth", "#B05A78", "#D884A2"),

        ("DayOffShade",  "#EFE6D6", "#2B2620"),
        ("DayOffRed",    "#B0352B", "#F0806F"),
        ("Mark",         "#6A4C9C", "#A98BE0"),

        // The day detail's cards, which were written into its XAML before there was a palette
        // to write them into.
        ("CardFill",     "#F3EEE3", "#24211B"),
        ("CardEdge",     "#DED5C4", "#3A352E"),
        ("CardText",     "#575044", "#C2BAAB"),
        ("Body",         "#4A4437", "#CFC8BA"),

        // What a message lays over the page it covers, at four tenths. Dark in both halves,
        // unlike every other pair: the ink of the dark theme is a light colour, and a layer
        // that lightens the page does not read as one that is out of reach.
        ("Scrim",        "#1C1A17", "#000000")
    ];

    /// <summary>The theme the resources were written for.</summary>
    public static AppTheme Applied { get; private set; } = AppTheme.Unspecified;

    /// <summary>
    /// What the system asks for. Unspecified is light: an app that cannot tell should look the
    /// way it has always looked.
    /// </summary>
    public static AppTheme Wanted =>
        Application.Current?.RequestedTheme == AppTheme.Dark ? AppTheme.Dark : AppTheme.Light;

    public static bool HasChanged => Applied != Wanted;

    /// <summary>
    /// The active value of one pair, for the few places that cannot ask the XAML for it — the
    /// system bars, which Android paints itself.
    /// </summary>
    public static Color Colour(string key) =>
        Application.Current?.Resources.TryGetValue(key, out object? value) == true && value is Color colour
            ? colour
            : Color.FromArgb(Colours.First(c => c.Key == key).Light);

    /// <summary>
    /// Writes the active half of every pair into the resources, before any page is built. The
    /// XAML asks for these with DynamicResource, so a page already on screen follows; the views
    /// built in code read the colour once, which is why a change of theme builds them again.
    /// </summary>
    public static void Apply(ResourceDictionary resources)
    {
        bool dark = Wanted == AppTheme.Dark;
        Applied = Wanted;

        foreach (var (key, light, night) in Colours)
            resources[key] = Color.FromArgb(dark ? night : light);
    }
}
