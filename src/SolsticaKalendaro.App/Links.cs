namespace SolsticaKalendaro.App;

/// <summary>
/// Where the app sends a reader who wants more than the app. The article's own link is not
/// here: it belongs to the document the core implements, and lives beside the version it
/// belongs to, in <see cref="SolsticaKalendaro.Core.SolsticaCalendar.SpecificationUrl"/>.
/// </summary>
public static class Links
{
    public const string Source = "https://github.com/j-plaza1/solstica-kalendaro-app";

    /// <summary>
    /// Installed from an APK rather than from a store, the app hears about new versions from
    /// nowhere; this is the one place a reader can go and look.
    /// </summary>
    public const string Releases = "https://github.com/j-plaza1/solstica-kalendaro-app/releases";

    public static Task Open(string url) => Launcher.Default.OpenAsync(new Uri(url));
}
