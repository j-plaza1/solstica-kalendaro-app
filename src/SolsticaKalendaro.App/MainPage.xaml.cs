using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

public partial class MainPage : ContentPage
{
    private static readonly SolsticaCalendar Cal = new(SolsticaEpoch.Expository2026);

    private int _today = -1;

    public MainPage()
    {
        InitializeComponent();
        NameTheWeekdays();
        Show(DateOnly.FromDateTime(DateTime.Now));
    }

    private void Show(DateOnly today)
    {
        // The calendar begins at its epoch. Before that there is no year to be in, so the
        // view opens on the first year it covers and says how long the wait is.
        var outline = Cal.Outline(Cal.Epoch.FirstSolsticaYear);
        var opens = FirstGregorianOf(outline);
        int year = Cal.Epoch.FirstSolsticaYear;

        if (opens is { } start && today < start)
        {
            int days = start.DayNumber - today.DayNumber;
            SubtitleLabel.Text = $"Encara no ha començat · falten {days} {(days == 1 ? "dia" : "dies")}";
        }
        else
        {
            year = Cal.FromGregorian(today).Year;
            outline = Cal.Outline(year);
            SubtitleLabel.IsVisible = false;
        }

        YearLabel.Text = year.ToString(YearView.Culture);

        var rows = YearView.Build(outline, today, Palette());
        Rows.ItemsSource = rows;

        // No today to go to in a year the calendar has not reached yet.
        _today = YearView.IndexOfToday(rows);
        TodayButton.IsEnabled = _today >= 0;
        TodayButton.Opacity = _today >= 0 ? 1 : 0.4;
    }

    private void OnTodayClicked(object? sender, EventArgs e)
    {
        if (_today >= 0) Rows.ScrollTo(_today, position: ScrollToPosition.Center, animate: true);
    }

    /// <summary>
    /// The Gregorian date the year opens on, taken from the outline rather than worked out
    /// here. Null only past what DateOnly can represent, which the first day never is.
    /// </summary>
    private static DateOnly? FirstGregorianOf(IReadOnlyList<OutlineRow> outline) =>
        outline.OfType<WeekRow>().FirstOrDefault()?.Days[0].Gregorian;

    /// <summary>
    /// Monday to Sunday: the Solstica week, which every block of the year begins on. It is not
    /// the Gregorian week of the same days, and after the first Jarfino the two do not agree.
    /// </summary>
    private void NameTheWeekdays()
    {
        Label[] slots = [Weekday0, Weekday1, Weekday2, Weekday3, Weekday4, Weekday5, Weekday6];
        var names = YearView.Culture.DateTimeFormat.AbbreviatedDayNames;

        for (int i = 0; i < slots.Length; i++)
        {
            var day = (DayOfWeek)(((int)DayOfWeek.Monday + i) % 7);
            slots[i].Text = names[(int)day].TrimEnd('.').ToUpper(YearView.Culture);
            slots[i].FontFamily = "PlexSemiBold";
            slots[i].FontSize = 10.5;
            slots[i].CharacterSpacing = 0.6;
            slots[i].HorizontalTextAlignment = TextAlignment.Center;
            slots[i].TextColor = (Color)Application.Current!.Resources["Muted"];
        }
    }

    private YearPalette Palette() => new(
        (Color)Application.Current!.Resources["SeasonFirst"],
        (Color)Application.Current!.Resources["SeasonSecond"],
        (Color)Application.Current!.Resources["SeasonThird"],
        (Color)Application.Current!.Resources["SeasonFourth"]);
}
