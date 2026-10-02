namespace SolsticaKalendaro.App;

/// <summary>
/// Building the pages again when something they were built from has changed underneath them —
/// the language, the date the calendar begins, the system's text size, the system's theme. The
/// views made in code read their strings, sizes and colours once, so they have to be made
/// again; what must not change is where the reader is standing.
/// </summary>
public static class Rebuild
{
    /// <summary>
    /// The year the reader was on, at the row they had reached, and Options behind it at the
    /// same scroll if that is where they were. Any other page on top — a day, a period, About,
    /// the panel — is left behind: the year underneath is what has to survive.
    /// </summary>
    public static void WhereTheReaderStands()
    {
        if (Application.Current is not { } app || app.Windows.Count == 0) return;

        // A message belongs to the page it covers, and that page is about to be gone.
        MessageOverlay.Dismiss();

        var window = app.Windows[0];
        var stack = window.Page is NavigationPage open
            ? open.Navigation.NavigationStack
            : [];

        var place = stack.OfType<MainPage>().FirstOrDefault()?.Place;
        double? scroll = stack.OfType<OptionsPage>().FirstOrDefault()?.Scroll;

        var navigation = new NavigationPage(new MainPage(place));
        window.Page = navigation;

        if (scroll is { } y) navigation.PushAsync(new OptionsPage(y), animated: false);
    }
}
