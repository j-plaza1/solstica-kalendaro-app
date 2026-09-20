namespace SolsticaKalendaro.Core;

/// <summary>
/// A date in the Solstica Kalendaro. <paramref name="Day"/> is 1 for the extra-weekly days
/// (Jarfino, Supertago). The block a date falls in has a fixed name in every period, but its
/// length — and therefore which days exist — depends on the period's block arrangement.
/// </summary>
public readonly record struct SolsticaDate(int Year, PeriodKind Period, int Day)
    : IComparable<SolsticaDate>
{
    public static SolsticaDate Jarfino(int year) => new(year, PeriodKind.Jarfino, 1);
    public static SolsticaDate Supertago(int year) => new(year, PeriodKind.Supertago, 1);

    public bool IsValid
    {
        get
        {
            if (!ValidityPeriod.IsTabulated(Year)) return false;
            if (Period == PeriodKind.Supertago) return Day == 1 && SolsticaCalendar.IsLeapYear(Year);
            return Day >= 1 && Day <= ValidityPeriod.For(Year).Layout.Length(Period);
        }
    }

    /// <summary>
    /// The universal holidays of section 8.3. The Rekomenco is defined astronomically — the Monday
    /// immediately following the 90-degree solstice — so it is resolved through the period rather
    /// than hard-coded: it is 8 Jarmezo today and 1 Sepa from the structural reform of 3324.
    /// </summary>
    public bool IsFestivity => FestivityName is not null;

    public string? FestivityName
    {
        get
        {
            if (Period == PeriodKind.Jarfino) return "Jarfino";
            if (Period == PeriodKind.Supertago) return "Supertago";
            if (Period == PeriodKind.Unua && Day == 1) return "Jarkomenco";
            return ValidityPeriod.For(Year).Rekomenco == (Period, Day) ? "Rekomenco" : null;
        }
    }

    public int CompareTo(SolsticaDate other)
    {
        int y = Year.CompareTo(other.Year);
        return y != 0 ? y : SolsticaCalendar.DayOfYear(this).CompareTo(SolsticaCalendar.DayOfYear(other));
    }

    /// <summary>Canonical form, e.g. "12 Kvara 2027" or "Jarfino 2027".</summary>
    public override string ToString() =>
        Period.IsExtraWeekly() ? $"{Period.Name()} {Year}" : $"{Day} {Period.Name()} {Year}";
}
