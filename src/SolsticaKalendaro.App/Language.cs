using System.Globalization;
using SolsticaKalendaro.App.Resources.Strings;

namespace SolsticaKalendaro.App;

/// <summary>
/// Which of the four languages the app speaks, and where its dates take their shape from.
///
/// The device's language is used when it is one of the four and English otherwise, because a
/// reader who understands none of them is better served by the one most likely to be a second
/// language than by the one this was written in.
/// </summary>
public static class Language
{
    /// <summary>Follow the device, whatever it says.</summary>
    public const string Automatic = "auto";

    private const string Fallback = "en";
    private const string Preference = "language";

    /// <summary>The four, in the order the picker offers them.</summary>
    public static readonly string[] Codes = ["ca", "es", "en", "eo"];

    /// <summary>
    /// Read before anything is changed, so that "automatic" still has something to mean after
    /// the app has set a culture of its own.
    /// </summary>
    private static readonly CultureInfo Device = CultureInfo.CurrentCulture;

    public static CultureInfo Culture { get; private set; } = Device;

    /// <summary>The code the reader chose, or <see cref="Automatic"/>.</summary>
    public static string Chosen => Preferences.Get(Preference, Automatic);

    public static void Choose(string code)
    {
        Preferences.Set(Preference, code);
        Apply();
    }

    public static void Apply()
    {
        Culture = Resolve(Chosen);

        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
        AppStrings.Culture = Culture;
    }

    /// <summary>
    /// The device's own culture when it speaks the chosen language, so that a reader on a
    /// Catalan phone gets Catalan dates as that phone writes them, rather than a generic
    /// Catalan. Otherwise the language on its own.
    /// </summary>
    private static CultureInfo Resolve(string chosen)
    {
        string code = chosen == Automatic ? Device.TwoLetterISOLanguageName : chosen;
        if (!Codes.Contains(code)) code = Fallback;

        return Device.TwoLetterISOLanguageName == code ? Device : CultureInfo.GetCultureInfo(code);
    }

    /// <summary>The name of a language as its own speakers say it, in the language of the app.</summary>
    public static string NameOf(string code) => code switch
    {
        "ca" => AppStrings.LanguageCatalan,
        "es" => AppStrings.LanguageSpanish,
        "en" => AppStrings.LanguageEnglish,
        "eo" => AppStrings.LanguageEsperanto,
        Automatic => AppStrings.LanguageAutomatic,
        _ => code
    };
}
