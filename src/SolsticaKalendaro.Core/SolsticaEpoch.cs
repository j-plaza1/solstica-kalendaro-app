namespace SolsticaKalendaro.Core;

/// <summary>
/// Fixes the correspondence between the two calendars. 1 Unua of Solstica year
/// <paramref name="FirstSolsticaYear"/> falls on <paramref name="AnchorMonth"/>/<paramref name="AnchorDay"/>
/// of the preceding Gregorian year.
///
/// Because both calendars use the 4/100/400 rule with the same year numbering, and
/// because 29 February of year Y always falls inside Solstica year Y, the two stay in
/// permanent lockstep: 1 Unua of *every* subsequent year falls on the same Gregorian
/// month and day. The epoch therefore fully determines the mapping, forever.
/// </summary>
public sealed record SolsticaEpoch(int AnchorMonth, int AnchorDay, int FirstSolsticaYear)
{
    /// <summary>Gregorian date of 1 Unua of the given Solstica year.</summary>
    public DateOnly YearStart(int solsticaYear) =>
        new(solsticaYear - 1, AnchorMonth, AnchorDay);

    public DateOnly AdoptionDate => YearStart(FirstSolsticaYear);

    /// <summary>Gregorian year in which the transition happens.</summary>
    public int AdoptionYear => FirstSolsticaYear - 1;

    /// <summary>
    /// True when the anchor is not 21 December. The 2031 window is the notable case:
    /// its solstice falls on 22 December UTC, so that epoch yields a correspondence
    /// permanently one day later than all the others. Surface this in any epoch picker.
    /// </summary>
    public bool IsOffAnchor => !(AnchorMonth == 12 && AnchorDay == 21);

    /// <summary>Expository epoch used by the proposal document. Not a proposed adoption date.</summary>
    public static readonly SolsticaEpoch Expository2026 = new(12, 21, 2027);

    /// <summary>
    /// Adoption windows from section 7.6: Gregorian years in which the December
    /// solstice falls on a Monday, so that every block of the calendar begins on a Monday.
    /// </summary>
    public static readonly IReadOnlyList<SolsticaEpoch> AdoptionWindows =
    [
        new(12, 22, 2032), // solstice 22 Dec 2031, 01:55 UTC
        new(12, 21, 2038), // solstice 21 Dec 2037, 13:07 UTC
        new(12, 21, 2049), // solstice 21 Dec 2048, 05:02 UTC
        new(12, 21, 2055), // solstice 21 Dec 2054, 16:10 UTC
        new(12, 21, 2066), // solstice 21 Dec 2065, 08:01 UTC
        new(12, 21, 2072), // solstice 21 Dec 2071, 19:04 UTC
        new(12, 21, 2077)  // solstice 21 Dec 2076, 00:14 UTC
    ];

    public override string ToString() =>
        $"{AdoptionDate:yyyy-MM-dd} = 1 Unua {FirstSolsticaYear}";
}
