using System.Globalization;
using SolsticaKalendaro.App.Resources.Strings;

namespace SolsticaKalendaro.App;

/// <summary>
/// How a Gregorian date is written, in each of the four languages the app speaks. The shapes
/// themselves are the platform's — ".NET's Catalan writes 14 de novembre" — and what is here is
/// the little that the platform cannot be asked for: Esperanto's ordinal dates, and its months.
///
/// Every method takes the culture it is to write in, and reads its one template with that same
/// culture, rather than asking <see cref="Language"/> for the language in force.
/// <see cref="Language"/> reads the reader's stored choice and so reaches MAUI, and this file is
/// compiled by link into tests/SolsticaKalendaro.App.Tests, where there is no MAUI and no device
/// to have a choice on. <see cref="Text"/> holds the wrappers that pass the language in force.
/// </summary>
public static class GregorianDates
{
    private const string EsperantoCode = "eo";

    /// <summary>
    /// Esperanto's months, written here rather than read from the platform.
    ///
    /// Esperanto writes a month inside a date in lower case, and the platform's answer is not
    /// dependable: .NET's own culture data has written them capitalised, and the data an Android
    /// device carries need not be the data the machine that built the app carries. Twelve names
    /// that have not moved since 1887 are a smaller thing to keep here than a difference between
    /// two devices is to chase.
    ///
    /// Thirteen entries because <see cref="DateTimeFormatInfo.MonthNames"/> asks for thirteen:
    /// the last is the thirteenth month of calendars that have one, and this is not one.
    /// </summary>
    private static readonly string[] EsperantoMonths =
    [
        "januaro", "februaro", "marto", "aprilo", "majo", "junio",
        "julio", "aŭgusto", "septembro", "oktobro", "novembro", "decembro", ""
    ];

    /// <summary>The same twelve shortened, as the platform shortens them, in lower case.</summary>
    private static readonly string[] EsperantoAbbreviatedMonths =
    [
        "jan", "feb", "mar", "apr", "maj", "jun",
        "jul", "aŭg", "sep", "okt", "nov", "dec", ""
    ];

    /// <summary>
    /// Esperanto with the months above in place of the platform's, and nothing else altered: the
    /// weekdays, the date patterns and the numbers stay the platform's, so a date keeps the shape
    /// .NET gives it in Esperanto and only the spelling of the month is the app's own.
    ///
    /// This is what every screen that writes an Esperanto date goes through, including the ones
    /// that only hand a format string to .NET — the Gregorian heading of the day detail and the
    /// date picker's field, both of which ask for "D" — so those need no Esperanto in them.
    /// </summary>
    public static CultureInfo Esperanto { get; } = BuildEsperanto();

    private static CultureInfo BuildEsperanto()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(EsperantoCode).Clone();
        DateTimeFormatInfo format = culture.DateTimeFormat;

        format.MonthNames = EsperantoMonths;
        format.MonthGenitiveNames = EsperantoMonths;
        format.AbbreviatedMonthNames = EsperantoAbbreviatedMonths;
        format.AbbreviatedMonthGenitiveNames = EsperantoAbbreviatedMonths;

        return CultureInfo.ReadOnly(culture);
    }

    /// <summary>Whichever Esperanto the culture is — the app's own or a device set to it.</summary>
    public static bool IsEsperanto(CultureInfo culture) =>
        culture.TwoLetterISOLanguageName == EsperantoCode;

    /// <summary>
    /// "divendres, 14 de novembre de 2031": the whole date with its weekday, as the language
    /// writes it at length. The shape is .NET's; in Esperanto the month inside it is ours.
    /// </summary>
    public static string Full(DateOnly date, CultureInfo culture) => date.ToString("D", culture);

    /// <summary>
    /// "14 de novembre del 2031", "la 14-a de novembro 2031": the date where it stands on its own,
    /// or after a preposition — "je la 14-a de novembro 2031".
    /// </summary>
    public static string Long(DateOnly date, CultureInfo culture) =>
        WithYear(date, culture, accusative: false);

    /// <summary>
    /// The same date where the sentence puts it as a bare answer to "when?": "comença el 21 de
    /// desembre del 2026". Esperanto marks that with the accusative — "la 21-an de decembro 2026"
    /// and not "la 21-a" — and the other three languages have the one form, so for them this is
    /// <see cref="Long"/> again.
    /// </summary>
    public static string LongAccusative(DateOnly date, CultureInfo culture) =>
        WithYear(date, culture, accusative: true);

    private static string WithYear(DateOnly date, CultureInfo culture, bool accusative)
    {
        string dayAndMonth = IsEsperanto(culture)
            ? EsperantoOrdinal(date, culture, accusative)
            : date.ToString("d MMMM", culture);

        return string.Format(culture, Template(nameof(AppStrings.DateWithYear), culture),
                             dayAndMonth, date.Year);
    }

    /// <summary>
    /// "la 14-a de novembro", "la 14-an de novembro".
    ///
    /// The article is part of the date and not of the sentence around it. Esperanto writes an
    /// ordinal date with it wherever the date appears, and a template that supplied it would have
    /// to know which of the two forms was coming; so the four Esperanto templates that take a
    /// date say "komenciĝi {0}" and not "komenciĝi la {0}". The other three languages leave their
    /// article to the template, which is where it belongs for them: it varies with the sentence
    /// and not with the date.
    /// </summary>
    private static string EsperantoOrdinal(DateOnly date, CultureInfo culture, bool accusative) =>
        $"la {date.Day.ToString(culture)}-{(accusative ? "an" : "a")} de {EsperantoMonths[date.Month - 1]}";

    /// <summary>
    /// "14 novembro": the day and its month with no year, for the band of a Jarfino or a
    /// Supertago, which carries the year above it.
    /// </summary>
    public static string DayAndMonth(DateOnly date, CultureInfo culture) =>
        $"{date.Day.ToString(culture)} {MonthName(culture.DateTimeFormat.MonthNames, date.Month)}";

    /// <summary>"14 nov": the same shortened, for a cell of the grid, which has less room.</summary>
    public static string DayAndShortMonth(DateOnly date, CultureInfo culture) =>
        $"{date.Day.ToString(culture)} {MonthName(culture.DateTimeFormat.AbbreviatedMonthNames, date.Month)}";

    /// <summary>
    /// A bare month name. Catalan's date patterns take the genitive, which drags a "de" along
    /// that a calendar cell has no room for and does not need beside a number.
    /// </summary>
    private static string MonthName(string[] names, int month)
    {
        string name = names[month - 1].TrimEnd('.');
        if (name.StartsWith("de ", StringComparison.OrdinalIgnoreCase)) name = name[3..];
        if (name.StartsWith("d'", StringComparison.OrdinalIgnoreCase)) name = name[2..];
        return name;
    }

    /// <summary>
    /// One of the string table's templates, read in the language asked for rather than in the one
    /// the app is set to. <see cref="AppStrings"/> keeps a culture of its own, which
    /// <see cref="Language"/> sets; going round it is what lets these methods be pure in their
    /// arguments, and so lets four languages be read in one test run.
    /// </summary>
    private static string Template(string name, CultureInfo culture) =>
        AppStrings.ResourceManager.GetString(name, culture)
        ?? throw new InvalidOperationException($"The string table has no {name}.");
}
