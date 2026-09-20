namespace SolsticaKalendaro.Core;

/// <summary>
/// The four seasons, numbered as in the document. Each opens at the cardinal point given by
/// <see cref="SeasonExtensions.OpeningLongitude"/>; the hemisphere names are deliberately
/// avoided, since the calendar is global (section 8.1).
/// </summary>
public enum Season
{
    First = 1,
    Second,
    Third,
    Fourth
}

public static class SeasonExtensions
{
    /// <summary>Ecliptic longitude of the cardinal point that opens the season.</summary>
    public static int OpeningLongitude(this Season s) => s switch
    {
        Season.First => 270,
        Season.Second => 0,
        Season.Third => 90,
        Season.Fourth => 180,
        _ => throw new ArgumentOutOfRangeException(nameof(s))
    };
}

/// <summary>
/// Integer allocation of the 365 days among the four seasons. The transition blocks do not
/// belong to a single season: their days are split, and calibrating that split is the
/// mechanism of section 4.5. Recalibrating it is *semantic* — it moves no date (section 9.4).
/// </summary>
public sealed record SeasonAllocation(int First, int Second, int Third, int Fourth)
{
    public const int Total = 365;

    public int this[Season s] => s switch
    {
        Season.First => First,
        Season.Second => Second,
        Season.Third => Third,
        Season.Fourth => Fourth,
        _ => throw new ArgumentOutOfRangeException(nameof(s))
    };

    public bool IsWellFormed => First + Second + Third + Fourth == Total;

    /// <summary>Season containing the given common-year ordinal (the Supertago aside).</summary>
    public Season SeasonOf(int commonOrdinal)
    {
        if (commonOrdinal is < 1 or > Total)
            throw new ArgumentOutOfRangeException(nameof(commonOrdinal));
        int cumulative = 0;
        foreach (var s in Enum.GetValues<Season>())
        {
            cumulative += this[s];
            if (commonOrdinal <= cumulative) return s;
        }
        throw new InvalidOperationException("Unreachable.");
    }

    public override string ToString() => $"{First}/{Second}/{Third}/{Fourth}";
}

/// <summary>
/// One row of the table of section 9.3. A validity period fixes the seasonal allocation, the
/// transition-block arrangement and the seam at which the Supertago falls. Conversion must know
/// which period it is working in, but <b>only for leap years</b>: in common years every period
/// with the same block arrangement produces identical dates (section 9.4).
/// </summary>
/// <param name="SupertagoSeam">
/// Common-year ordinal of the last day <i>before</i> the intercalation. The Supertago itself
/// therefore occupies ordinal <c>SupertagoSeam + 1</c>. Always a multiple of seven, because the
/// rule of section 5.1 places it at a Sunday|Monday seam.
/// </param>
/// <param name="AllocationError">Maximum seasonal error of the allocation alone, in days.</param>
/// <param name="ResidualError">What remains once the Supertago is counted, on the criterion of section 5.1.</param>
public sealed record ValidityPeriod(
    int FirstYear,
    int LastYear,
    SeasonAllocation Allocation,
    BlockWidths Blocks,
    int SupertagoSeam,
    double AllocationError,
    double ResidualError)
{
    public YearLayout Layout => YearLayout.For(Blocks);

    /// <summary>
    /// Common-year ordinal at which the 90-degree solstice falls: the seam between this day and
    /// the next, by the seasonal allocation.
    /// </summary>
    public int NinetyDegreeSeam => Allocation.First + Allocation.Second;

    /// <summary>
    /// The Rekomenco (sections 6.2 and 8.3): the Monday immediately following the 90-degree
    /// solstice, as a common-year ordinal. Computed from the rule rather than tabulated.
    ///
    /// It comes out as 183 in every period of the window, because the first transition block is
    /// seven days wide throughout, so the Jarmezo always opens on day 176 and always contains the
    /// solstice; the next Monday is therefore always the opening of the following block. What
    /// changes is only the <i>name</i> of that day: 8 Jarmezo under 7/14/7, and 1 Sepa from the
    /// structural reform of 3324 onwards.
    /// </summary>
    public int RekomencoCommonOrdinal
    {
        get
        {
            int n = NinetyDegreeSeam + 1;
            while ((n - 1) % 7 != 0) n++;   // advance to the next Monday
            return n;
        }
    }

    /// <summary>The Rekomenco as a named date. Extra-weekly days never carry the festivity.</summary>
    public (PeriodKind Block, int Day) Rekomenco => Layout.FromCommonOrdinal(RekomencoCommonOrdinal);

    /// <summary>Ordinal occupied by the Supertago in a leap year.</summary>
    public int SupertagoOrdinal => SupertagoSeam + 1;

    /// <summary>The deficient season the Supertago is assigned to.</summary>
    public Season SupertagoSeason => Allocation.SeasonOf(SupertagoOrdinal);

    public bool Contains(int year) => year >= FirstYear && year <= LastYear;

    /// <summary>
    /// True where the intercalated day does not reduce the maximum error, only the RMS: the
    /// dominant season is over-allocated and the extra day can do nothing about it (section 9.3).
    /// </summary>
    public bool SupertagoIsIneffective => ResidualError >= AllocationError;

    /// <summary>Days in the given season, counting the Supertago in leap years.</summary>
    public int SeasonLength(Season season, int year) =>
        Allocation[season] + (SolsticaCalendar.IsLeapYear(year) && season == SupertagoSeason ? 1 : 0);

    /// <summary>
    /// The eleven periods of section 9.3, covering 2000–10000. The horizon is the secular
    /// model's, not the calendar's: going further would need a long-term orbital solution.
    /// </summary>
    public static readonly IReadOnlyList<ValidityPeriod> Table =
    [
        //                                allocation                     blocks                        seam   err    resid
        new(2000, 2095, new(89, 93, 93, 90), BlockWidths.SevenFourteenSeven, 182, 0.668, 0.418), // mid-Jarmezo
        new(2096, 3150, new(89, 92, 94, 90), BlockWidths.SevenFourteenSeven, 280, 0.313, 0.274), // after Ekvinokso II
        new(3151, 3323, new(88, 92, 94, 91), BlockWidths.SevenFourteenSeven,  84, 0.719, 0.469), // before Ekvinokso I
        new(3324, 4502, new(89, 91, 94, 91), BlockWidths.SevenSevenFourteen, 280, 0.332, 0.261), // structural reform
        new(4503, 5505, new(89, 90, 94, 92), BlockWidths.SevenSevenFourteen, 175, 0.438, 0.314), // before Jarmezo
        new(5506, 6429, new(89, 90, 93, 93), BlockWidths.SevenSevenFourteen,  84, 0.370, 0.262),
        new(6430, 7417, new(90, 89, 93, 93), BlockWidths.SevenSevenFourteen, 175, 0.425, 0.304),
        new(7418, 7721, new(90, 89, 92, 94), BlockWidths.SevenSevenFourteen,  84, 0.595, 0.595),
        new(7722, 8653, new(91, 89, 92, 93), BlockWidths.SevenSevenFourteen, 273, 0.491, 0.241), // mid-Ekvinokso II
        new(8654, 9843, new(91, 89, 91, 94), BlockWidths.SevenSevenFourteen,  84, 0.559, 0.559),
        new(9844, 10000, new(91, 90, 91, 93), BlockWidths.SevenSevenFourteen, 84, 0.906, 0.656)
    ];

    /// <summary>The period in force today, and the one the app runs in by default.</summary>
    public static ValidityPeriod Current => Table[0];

    /// <summary>The single structural reform of the whole window (section 9.4).</summary>
    public const int StructuralReformYear = 3324;

    /// <summary>
    /// Year from which the cardinal points can no longer be contained by the transition blocks
    /// under any arrangement (section 9.6). The calendar keeps working; the containment property
    /// of section 4.5 is simply lost, and no recalibration restores it.
    /// </summary>
    public const int ContainmentEndsYear = 8537;

    public static ValidityPeriod For(int year) =>
        Table.FirstOrDefault(p => p.Contains(year))
        ?? throw new ArgumentOutOfRangeException(nameof(year),
            $"Year {year} lies outside the tabulated window {Table[0].FirstYear}–{Table[^1].LastYear}.");

    public static bool IsTabulated(int year) => Table.Any(p => p.Contains(year));

    public override string ToString() => $"{FirstYear}–{LastYear} {Allocation} blocks {Blocks} Supertago at {SupertagoOrdinal}";
}
