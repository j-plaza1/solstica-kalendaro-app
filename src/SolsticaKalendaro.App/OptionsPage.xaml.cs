using SolsticaKalendaro.App.Resources.Strings;
using SolsticaKalendaro.Core;

namespace SolsticaKalendaro.App;

/// <summary>
/// What the reader can decide: the language, and when the calendar begins. #20 adds the rest
/// days, which is why this is a page rather than a dialog.
///
/// Both choices rebuild the pages, because everything already drawn was drawn with the old
/// answer. Both keep the reader where they were: on this page, scrolled to where they were
/// reading it, and behind it the year they were reading, at the row they had reached.
/// </summary>
public partial class OptionsPage : ContentPage
{
    private readonly double _resumeScroll;

    /// <param name="resumeScroll">
    /// How far down the reader had come, when this page is replacing one they were already
    /// looking at. The lists are longer than a phone screen, so the tick that has just moved
    /// may well be below the fold, and a page that opened at the top would hide the answer to
    /// what the reader just did.
    /// </param>
    public OptionsPage(double resumeScroll = 0)
    {
        InitializeComponent();
        _resumeScroll = resumeScroll;

        SemanticProperties.SetHint(BackButton, AppStrings.HintBack);
        TitleLabel.Text = AppStrings.MenuOptions;
        LanguageHeading.Text = AppStrings.Language.ToUpper(Language.Culture);
        CalendarBeginsHeading.Text = AppStrings.CalendarBegins.ToUpper(Language.Culture);
        CalendarBeginsNote.Text = AppStrings.CalendarBeginsNote;
        RestDaysHeading.Text = AppStrings.RestDaysHeading.ToUpper(Language.Culture);

        ShowLanguages();
        ShowStarts();
        ShowRestDays();

        // Loaded comes too early: the page exists but nothing has been measured, and a scroll
        // to a position the content does not yet reach is clamped to the top. The content's
        // own height is what decides, and it arrives in more than one pass as the lists are
        // measured, so the place is followed until the content is tall enough to hold it.
        if (_resumeScroll > 0) Column.SizeChanged += RestoreScroll;
    }

    /// <summary>
    /// Back to where the reader was. Called again while the content is still growing — a
    /// position beyond what has been measured is clamped to the bottom of it — and let go of as
    /// soon as the content can hold the place, after which the scrolling is the reader's.
    /// </summary>
    private void RestoreScroll(object? sender, EventArgs e)
    {
        if (Scroller.Height <= 0 || Column.Height <= 0) return;

        double furthest = Math.Max(0, Column.Height - Scroller.Height);
        if (furthest >= _resumeScroll) Column.SizeChanged -= RestoreScroll;

        Scroller.ScrollToAsync(0, Math.Min(_resumeScroll, furthest), animated: false);
    }

    private void OnBackClicked(object? sender, EventArgs e) => Navigation.PopAsync();

    private void ShowLanguages()
    {
        // Following the device comes first: it is what the app does until asked otherwise.
        foreach (string code in new[] { Language.Automatic }.Concat(Language.Codes))
            Languages.Add(ChoiceRow.Build(Language.NameOf(code), Language.Chosen == code, null,
                                          () => ChooseLanguage(code)));
    }

    private void ShowStarts()
    {
        foreach (var epoch in CalendarStart.Options)
            Starts.Add(ChoiceRow.Build(
                Text.LongDate(epoch.AdoptionDate),
                CalendarStart.Chosen.FirstSolsticaYear == epoch.FirstSolsticaYear,
                // Only one of them starts on a different day, and that is worth saying where
                // the reader is choosing rather than afterwards when every date has moved.
                epoch.IsOffAnchor ? AppStrings.SolsticeOn22 : null,
                () => ChooseStart(epoch)));
    }

    /// <summary>
    /// The seven days of the Solstica week, Monday first. Unlike the lists above this is not a
    /// choice of one: every day chosen carries a tick, and choosing none is allowed.
    /// </summary>
    private void ShowRestDays()
    {
        var chosen = RestDays.Chosen;

        for (int i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)(((int)DayOfWeek.Monday + i) % 7);
            Rests.Add(ChoiceRow.Build(Text.WeekDayTitle(day), chosen.Contains(day), null,
                                      () => ChooseRestDay(day)));
        }
    }

    private void ChooseRestDay(DayOfWeek day)
    {
        RestDays.Toggle(day);
        Rebuild();
    }

    private void ChooseLanguage(string code)
    {
        if (Language.Chosen == code) return;
        Language.Choose(code);
        Rebuild();
    }

    private void ChooseStart(SolsticaEpoch epoch)
    {
        if (CalendarStart.Chosen.FirstSolsticaYear == epoch.FirstSolsticaYear) return;
        CalendarStart.Choose(epoch);
        Rebuild();
    }

    /// <summary>
    /// Everything on screen was built with the old answer, so it is all built again — and both
    /// pages are told where they were: this one so the reader can see the tick that has just
    /// moved, the year underneath so that coming back is not a return to the top.
    /// </summary>
    private void Rebuild()
    {
        var place = Navigation.NavigationStack.OfType<MainPage>().FirstOrDefault()?.Place;

        var navigation = new NavigationPage(new MainPage(place));
        Application.Current!.Windows[0].Page = navigation;
        navigation.PushAsync(new OptionsPage(Scroller.ScrollY), animated: false);
    }
}
