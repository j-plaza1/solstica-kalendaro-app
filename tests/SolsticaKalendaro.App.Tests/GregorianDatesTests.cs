using System.Globalization;
using SolsticaKalendaro.App.Resources.Strings;
using Xunit;

namespace SolsticaKalendaro.App.Tests;

/// <summary>
/// What every place in the app that shows a Gregorian date says, in each of the four languages.
///
/// The Esperanto rows are the ones under test: the day is an ordinal and the month is spelled out
/// and joined to it with "de", the ordinal takes the accusative where the date is a bare answer to
/// "when?", and the month is lower case. The Catalan, Spanish and English rows are here to say
/// that they did not move — their values are what the app showed before any of this, pinned so
/// that a change to the Esperanto cannot quietly reach them.
///
/// Sentences are formatted from the templates the app ships, read out of the four .resx files
/// linked into this project, rather than from copies of them.
/// </summary>
public class GregorianDatesTests
{
    /// <summary>A Friday, in a leap year, with the longest month name of the twelve.</summary>
    private static readonly DateOnly November = new(2031, 11, 14);

    /// <summary>The day the calendar begins on by default, which is every "begins on" below.</summary>
    private static readonly DateOnly Start = new(2026, 12, 21);

    /// <summary>
    /// The culture a language is written in, as <c>Language.Resolve</c> picks it for a reader whose
    /// device speaks none of the four. Esperanto is the app's own rather than the platform's.
    /// </summary>
    private static CultureInfo CultureFor(string code) =>
        code == "eo" ? GregorianDates.Esperanto : CultureInfo.GetCultureInfo(code);

    /// <summary>A template as the app would read it, in the language asked for.</summary>
    private static string Template(string name, CultureInfo culture) =>
        AppStrings.ResourceManager.GetString(name, culture)
        ?? throw new InvalidOperationException($"The string table has no {name}.");

    // ── The year view, "Begins on …" ────────────────────────────────────────────────────────────
    // A bare time complement: Esperanto's accusative, and the article now comes with the date.

    [Theory]
    [InlineData("eo", "Komenciĝas la 21-an de decembro 2026")]
    [InlineData("ca", "Comença el 21 de desembre del 2026")]
    [InlineData("es", "Empieza el 21 diciembre de 2026")]
    [InlineData("en", "Begins on 21 December 2026")]
    public void StartsOn(string code, string expected)
    {
        CultureInfo culture = CultureFor(code);

        Assert.Equal(expected, string.Format(culture, Template(nameof(AppStrings.StartsOn), culture),
                                             GregorianDates.LongAccusative(Start, culture)));
    }

    // ── Today before the start, and Go to's two refusals ────────────────────────────────────────
    // The same date in the same case, in three sentences. Only their tail is asserted: the rest is
    // wording that this change does not own, and a sentence reworded elsewhere should not fail here.

    [Theory]
    [InlineData("eo", "por komenciĝi la 21-an de decembro 2026.")]
    [InlineData("ca", "perquè comenci el 21 de desembre del 2026.")]
    [InlineData("es", "para que empiece el 21 diciembre de 2026.")]
    [InlineData("en", "to begin on 21 December 2026.")]
    public void BeforeTheStart(string code, string expectedTail)
    {
        CultureInfo culture = CultureFor(code);
        string date = GregorianDates.LongAccusative(Start, culture);

        foreach (string name in new[] { nameof(AppStrings.TodayNotStarted),
                                        nameof(AppStrings.DateBeforeStart),
                                        nameof(AppStrings.YearBeforeStart) })
        {
            string sentence = string.Format(culture, Template(name, culture), date);

            Assert.EndsWith(expectedTail, sentence, StringComparison.Ordinal);
            Assert.DoesNotContain("{0}", sentence, StringComparison.Ordinal);
        }
    }

    // ── The day detail, "the next will be the Jarfino of 2031, on …" ────────────────────────────
    // After a preposition, so Esperanto writes the plain ordinal with its article: "je la 14-a".
    // The block's name is never translated, which is why "Jarfino" stands here as a literal.

    [Theory]
    [InlineData("eo", "la venonta estos la Jarfino de 2031 je la 14-a de novembro 2031.")]
    [InlineData("ca", "el pròxim serà el Jarfino de 2031, el 14 de novembre del 2031.")]
    [InlineData("es", "el próximo será el Jarfino de 2031, el 14 noviembre de 2031.")]
    [InlineData("en", "the next will be the Jarfino of 2031, on 14 November 2031.")]
    public void NextShift(string code, string expected)
    {
        CultureInfo culture = CultureFor(code);
        string shift = string.Format(culture, Template(nameof(AppStrings.ShiftName), culture),
                                     "Jarfino", 2031);

        Assert.Equal(expected, string.Format(culture, Template(nameof(AppStrings.NextShift), culture),
                                             shift, GregorianDates.Long(November, culture)));
    }

    // ── The date standing on its own ────────────────────────────────────────────────────────────
    // The festivity line of the day detail, after its "·", and every row of Options' list of start
    // dates. No sentence around it, so no accusative.

    [Theory]
    [InlineData("eo", "la 14-a de novembro 2031", "la 21-a de decembro 2026")]
    [InlineData("ca", "14 de novembre del 2031", "21 de desembre del 2026")]
    [InlineData("es", "14 noviembre de 2031", "21 diciembre de 2026")]
    [InlineData("en", "14 November 2031", "21 December 2026")]
    public void DateOnItsOwn(string code, string expectedNovember, string expectedStart)
    {
        CultureInfo culture = CultureFor(code);

        Assert.Equal(expectedNovember, GregorianDates.Long(November, culture));
        Assert.Equal(expectedStart, GregorianDates.Long(Start, culture));
    }

    /// <summary>
    /// Only Esperanto has two forms. Said here so that a language gaining one is a failure rather
    /// than a surprise on a screen.
    /// </summary>
    [Theory]
    [InlineData("ca")]
    [InlineData("es")]
    [InlineData("en")]
    public void OneFormOutsideEsperanto(string code)
    {
        CultureInfo culture = CultureFor(code);

        Assert.Equal(GregorianDates.Long(November, culture), GregorianDates.LongAccusative(November, culture));
        Assert.Equal(GregorianDates.Long(Start, culture), GregorianDates.LongAccusative(Start, culture));
    }

    // ── The Gregorian heading of the day detail, and the date picker's own field ────────────────
    // Both ask .NET for "D". The pattern and the weekday are the platform's, and the platform is
    // not the same everywhere: an Android device writes the Esperanto one with an article it has
    // here ("lundo, la 22-a de decembro 2031") and the Catalan one with "del 2031" where this
    // writes "de 2031". So what is pinned below is the shape .NET gives where these tests run, and
    // what is asserted of it is the month — the one part that is the app's own and therefore the
    // same on every device. A changed weekday or a changed separator here is the platform moving
    // under us and is worth looking at; it is not the app writing a date differently.

    [Theory]
    [InlineData("eo", "vendredo, 14-a de novembro 2031", "lundo, 21-a de decembro 2026")]
    [InlineData("ca", "divendres, 14 de novembre de 2031", "dilluns, 21 de desembre de 2026")]
    [InlineData("es", "viernes, 14 de noviembre de 2031", "lunes, 21 de diciembre de 2026")]
    [InlineData("en", "Friday, November 14, 2031", "Monday, December 21, 2026")]
    public void FullDate(string code, string expectedNovember, string expectedStart)
    {
        CultureInfo culture = CultureFor(code);

        Assert.Equal(expectedNovember, GregorianDates.Full(November, culture));
        Assert.Equal(expectedStart, GregorianDates.Full(Start, culture));
    }

    // ── The bands of the Jarfino and the Supertago, and a cell of the grid ──────────────────────
    // No year: the year is above them. The cell's is the shortened month, which the grid has room for.

    [Theory]
    [InlineData("eo", "14 novembro", "14 nov")]
    [InlineData("ca", "14 novembre", "14 nov")]
    [InlineData("es", "14 noviembre", "14 nov")]
    [InlineData("en", "14 November", "14 Nov")]
    public void DayAndMonthWithoutYear(string code, string expectedLong, string expectedShort)
    {
        CultureInfo culture = CultureFor(code);

        Assert.Equal(expectedLong, GregorianDates.DayAndMonth(November, culture));
        Assert.Equal(expectedShort, GregorianDates.DayAndShortMonth(November, culture));
    }

    // ── Esperanto's months are the app's own ────────────────────────────────────────────────────
    // The app writes them rather than reading them, because the platform's are not dependably lower
    // case and an Android device's culture data need not be the build machine's. Every one of the
    // twelve is checked in both formats that reach a screen, "D" and "d MMMM", and in the arrays
    // the date picker and the year view read.

    [Theory]
    [InlineData(1, "januaro", "jan")]
    [InlineData(2, "februaro", "feb")]
    [InlineData(3, "marto", "mar")]
    [InlineData(4, "aprilo", "apr")]
    [InlineData(5, "majo", "maj")]
    [InlineData(6, "junio", "jun")]
    [InlineData(7, "julio", "jul")]
    [InlineData(8, "aŭgusto", "aŭg")]
    [InlineData(9, "septembro", "sep")]
    [InlineData(10, "oktobro", "okt")]
    [InlineData(11, "novembro", "nov")]
    [InlineData(12, "decembro", "dec")]
    public void EsperantoMonthsAreLowerCase(int month, string name, string shortName)
    {
        CultureInfo eo = GregorianDates.Esperanto;
        var date = new DateOnly(2031, month, 14);

        Assert.Equal(name, name.ToLowerInvariant());
        Assert.Equal(shortName, shortName.ToLowerInvariant());

        // Lower case is proved by the match itself: "Novembro" does not contain "novembro".
        Assert.Contains(name, GregorianDates.Full(date, eo), StringComparison.Ordinal);
        Assert.Contains(name, date.ToString("d MMMM", eo), StringComparison.Ordinal);
        Assert.Contains(name, GregorianDates.Long(date, eo), StringComparison.Ordinal);
        Assert.Contains(name, GregorianDates.LongAccusative(date, eo), StringComparison.Ordinal);

        Assert.Equal(name, eo.DateTimeFormat.MonthNames[month - 1]);
        Assert.Equal(name, eo.DateTimeFormat.MonthGenitiveNames[month - 1]);
        Assert.Equal(shortName, eo.DateTimeFormat.AbbreviatedMonthNames[month - 1]);
        Assert.Equal(shortName, eo.DateTimeFormat.AbbreviatedMonthGenitiveNames[month - 1]);

        Assert.Equal($"14 {name}", GregorianDates.DayAndMonth(date, eo));
        Assert.Equal($"14 {shortName}", GregorianDates.DayAndShortMonth(date, eo));
    }

    /// <summary>
    /// The accusative is the only difference between the two forms, and it is on the ordinal and
    /// nowhere else. Asserted across a whole year so that no month or day number escapes it.
    /// </summary>
    [Fact]
    public void EsperantoAccusativeMarksTheOrdinalAlone()
    {
        CultureInfo eo = GregorianDates.Esperanto;

        for (var date = new DateOnly(2031, 1, 1); date.Year == 2031; date = date.AddDays(1))
        {
            string plain = GregorianDates.Long(date, eo);
            string accusative = GregorianDates.LongAccusative(date, eo);

            Assert.Equal($"la {date.Day}-a de {eo.DateTimeFormat.MonthNames[date.Month - 1]} {date.Year}", plain);
            Assert.Equal(plain.Replace($"{date.Day}-a de", $"{date.Day}-an de", StringComparison.Ordinal),
                         accusative);
        }
    }
}
