using System.Globalization;
using SolsticaKalendaro.App.Resources.Strings;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>
/// A year, typed. The Go to panel asks for one twice — on its own, and as part of a date — and
/// it behaves the same both times: the years there are written underneath, everything is
/// selected when the field takes focus so that typing replaces the year rather than extending
/// it, and a year the calendar does not have simply does not count as one.
///
/// It wraps controls the XAML declares rather than building them, so the look stays where the
/// rest of the look is.
/// </summary>
public sealed class YearField
{
    private readonly Entry _entry;

    public YearField(Entry entry, Label range)
    {
        _entry = entry;
        range.Text = string.Format(Language.Culture, AppStrings.YearRange, First, Last);

        entry.Focused += SelectEverything;
        entry.TextChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        entry.Completed += (_, _) => Entered?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The years there are: the calendar's own first, and the last the table covers.</summary>
    public static int First => CalendarStart.Chosen.FirstSolsticaYear;
    public static int Last => ValidityPeriod.Table[^1].LastYear;

    public event EventHandler? Changed;

    /// <summary>The keyboard's own key, which the panel treats as the button beside the field.</summary>
    public event EventHandler? Entered;

    /// <summary>
    /// What is typed, if it is a year the calendar has. Parsed as plain digits: the numeric
    /// keyboard gives nothing else, and a grouping separator or a sign would not be a year.
    /// </summary>
    public int? Year =>
        int.TryParse(_entry.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int year)
        && year >= First && year <= Last
            ? year
            : null;

    public void Show(int year) => _entry.Text = year.ToString(Language.Culture);

    public Task DismissKeyboard() => _entry.HideSoftInputAsync(CancellationToken.None);

    public void LetGo() => _entry.Unfocus();

    /// <summary>
    /// Android moves the caret itself as the field takes focus, so the selection has to be made
    /// after that rather than during it.
    /// </summary>
    private void SelectEverything(object? sender, FocusEventArgs e) =>
        _entry.Dispatcher.Dispatch(() =>
        {
            _entry.CursorPosition = 0;
            _entry.SelectionLength = _entry.Text?.Length ?? 0;
        });
}
