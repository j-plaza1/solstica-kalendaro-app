using Xunit;

namespace SolsticaKalendaro.Core.Tests;

/// <summary>
/// Which days are free. The rest days are days of the Solstica week, which after the first
/// Jarfino are not the Gregorian ones; the calendar's own festivities are free whatever the
/// reader has chosen, and the days outside the week never consult the choice at all.
/// </summary>
public class DayOffTests
{
    private static readonly IReadOnlySet<DayOfWeek> Weekend =
        new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };

    private static readonly IReadOnlySet<DayOfWeek> Never = new HashSet<DayOfWeek>();

    [Fact]
    public void AChosenWeekdayIsFreeAndTheOthersAreNot()
    {
        // 1 Unua is a Monday, so the week that follows names its own days: 6 and 7 Unua are the
        // Saturday and the Sunday, and 9 Unua a Monday that is nobody's festivity.
        var saturday = new SolsticaDate(2027, PeriodKind.Unua, 6);
        var sunday = new SolsticaDate(2027, PeriodKind.Unua, 7);
        var monday = new SolsticaDate(2027, PeriodKind.Unua, 8);

        Assert.Equal(DayOfWeek.Saturday, SolsticaCalendar.WeekDay(saturday));
        Assert.Equal(DayOfWeek.Sunday, SolsticaCalendar.WeekDay(sunday));
        Assert.Equal(DayOfWeek.Monday, SolsticaCalendar.WeekDay(monday));

        Assert.True(SolsticaCalendar.IsDayOff(saturday, Weekend));
        Assert.True(SolsticaCalendar.IsDayOff(sunday, Weekend));
        Assert.False(SolsticaCalendar.IsDayOff(monday, Weekend));
    }

    [Fact]
    public void ChoosingNoRestDayLeavesAnOrdinarySaturdayWorking()
    {
        var saturday = new SolsticaDate(2027, PeriodKind.Unua, 6);

        Assert.True(SolsticaCalendar.IsDayOff(saturday, Weekend));
        Assert.False(SolsticaCalendar.IsDayOff(saturday, Never));
    }

    [Fact]
    public void TheYearsOwnBeginningIsFreeWhateverTheReaderChose()
    {
        var jarkomenco = new SolsticaDate(2027, PeriodKind.Unua, 1);

        Assert.Equal("Jarkomenco", jarkomenco.FestivityName);
        Assert.True(SolsticaCalendar.IsDayOff(jarkomenco, Never));
    }

    [Theory]
    [InlineData(2027)]      // 8 Jarmezo, before the structural reform
    [InlineData(3400)]      // 1 Sepa, after it
    public void TheRekomencoIsFreeWhereverThePeriodPutsIt(int year)
    {
        var (block, day) = ValidityPeriod.For(year).Rekomenco;
        var rekomenco = new SolsticaDate(year, block, day);

        Assert.Equal("Rekomenco", rekomenco.FestivityName);
        Assert.Equal(year < ValidityPeriod.StructuralReformYear
            ? (PeriodKind.Jarmezo, 8)
            : (PeriodKind.Sepa, 1), (block, day));

        Assert.True(SolsticaCalendar.IsDayOff(rekomenco, Never));
    }

    [Fact]
    public void TheDaysOutsideTheWeekAreFreeWithoutConsultingTheChoice()
    {
        // They have no weekday to look up, so the set cannot decide anything about them.
        foreach (var date in new[] { SolsticaDate.Jarfino(2027), SolsticaDate.Supertago(2028) })
        {
            Assert.Null(SolsticaCalendar.WeekDay(date));
            Assert.True(SolsticaCalendar.IsDayOff(date, Never));
            Assert.True(SolsticaCalendar.IsDayOff(date, Weekend));
        }
    }
}
