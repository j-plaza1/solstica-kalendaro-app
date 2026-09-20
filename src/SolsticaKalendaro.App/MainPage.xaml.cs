using System.Globalization;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

public partial class MainPage : ContentPage
{
    private static readonly SolsticaCalendar Cal = new(SolsticaEpoch.Expository2026);

    public MainPage()
    {
        InitializeComponent();
        Show(DateOnly.FromDateTime(DateTime.Now));
    }

    private void Show(DateOnly today)
    {
        GregorianDateLabel.Text = today.ToString("d MMMM yyyy", CultureInfo.CurrentCulture);
        GregorianWeekDayLabel.Text = WeekDayName(today.DayOfWeek);
        EpochLabel.Text = $"Epoch: {Cal.Epoch}";

        // The calendar begins at its epoch, and today may be earlier: FromGregorian rejects
        // anything before 1 Unua of the epoch's first year. Say so, rather than converting
        // a date the calendar does not yet cover.
        var adoption = Cal.Epoch.AdoptionDate;
        if (today < adoption)
        {
            int days = adoption.DayNumber - today.DayNumber;
            SolsticaDateLabel.Text = "Not started yet";
            SolsticaWeekDayLabel.Text =
                $"1 Unua {Cal.Epoch.FirstSolsticaYear} arrives on "
                + $"{adoption.ToString("d MMMM yyyy", CultureInfo.CurrentCulture)}, "
                + $"{days} {(days == 1 ? "day" : "days")} from today";
            return;
        }

        var date = Cal.FromGregorian(today);
        SolsticaDateLabel.Text = date.ToString();
        SolsticaWeekDayLabel.Text = SolsticaCalendar.WeekDay(date) is { } weekDay
            ? WeekDayName(weekDay)
            : "Extra-weekly day: it pauses the seven-day cycle and has no weekday";
    }

    /// <summary>
    /// Both calendars' weekdays are named the same way, so that comparing them is meaningful:
    /// after the first Jarfino they drift apart, and that divergence is the point of the display.
    /// </summary>
    private static string WeekDayName(DayOfWeek day) =>
        CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(day);
}
