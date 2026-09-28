# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Requires the .NET 10 SDK. `Directory.Build.props` sets `TreatWarningsAsErrors` and
`EnforceCodeStyleInBuild` for every project, so a style violation fails the build.

```bash
dotnet test tests/SolsticaKalendaro.Core.Tests     # core library + test suite
dotnet test tests/SolsticaKalendaro.Core.Tests --filter FullyQualifiedName~EveryDayRoundTrips
dotnet build src/SolsticaKalendaro.App -f net10.0-android -t:Run   # deploy to a device
```

Scope build and test commands to a project rather than the solution: a bare
`dotnet build` drags in the MAUI app, which needs the `maui-android` workload and an
Android SDK. CI does the same, for the same reason.

**The Android build fails under a non-ASCII path** with `APT2265` — `aapt2` rejects
accents and emoji anywhere in the path, and the error names resources, not the path.
The core library and tests are unaffected.

CI (`.github/workflows/ci.yml`) runs on push to `main` and on PRs, in two jobs: one restores,
builds and tests the core in Release, scoped to the test project; the other installs the
`maui-android` workload and builds the app unsigned, which answers whether its resources and
fonts still pack. Neither builds the solution.

## Documentation ships with the code it describes

If a change makes this file or the README describe the code wrongly, correcting them is
part of that change — whatever the branch was scoped to. A branch boundary is about code,
not about the documentation that describes it, and a stale description is worse than an
absent one: it is read and believed.

## Versions say which document they implement

A release tag is `vN.M.O.P`. `N.M` is the version of the proposal document the app implements,
and `O.P` is the app's own. The core states the first half in
`SolsticaCalendar.SpecificationVersion`, currently `2.1`, and the release workflow refuses a tag
whose `N.M` disagrees with it rather than shipping an app that claims the wrong document.

`SolsticaCalendar.SpecificationUrl` sits beside it and holds that version's DOI — the version
one, not the concept one, because an app that names the document it implements has to point at
that document and not at whichever is newest. The two constants change in the same commit; the
period screen prints both, so a raised version with a stale link says so on screen.

- `ApplicationDisplayVersion` is `N.M.O.P`. `ApplicationVersion` is
  `N*1000000 + M*10000 + O*100 + P`, so no component may exceed 99; the workflow rejects one
  that does.
- `O = 0` means preliminary. Those releases are marked prerelease, and the first production
  release against a document is `vN.M.1.0`. `ReleaseVersion.IsPreview` states that rule once, in
  the core, so the About screen marks a build exactly where the workflow would; anything that is
  not four numbers is not a release version and is shown unmarked rather than throwing.

**The whole convention lives in `ReleaseVersion`, not in the workflow.** `ReleaseVersion.Of`
answers what a run is building — the display version, the code and whether it is a prerelease —
or says why a tag is not one, without throwing: a workflow wants a message to print. It is in
the core because the document's version is, so the check reads `SpecificationVersion` itself
rather than grepping the source for it. `tools/ReleaseVersion` is a console tool over it,
printing `display=`, `code=` and `prerelease=` for `$GITHUB_OUTPUT`:

```bash
dotnet run --project tools/ReleaseVersion -c Release -- tag v2.1.1.0
dotnet run --project tools/ReleaseVersion -c Release -- branch main    # a rehearsal, N.M.0.0
```

`release.yml` calls it, and **CI runs it on every pull request** — a production tag, a
preliminary one, a rehearsal, and two tags that must be refused — so the path no release has
taken yet is taken before the first one takes it. The rules were a shell block that ran only
while publishing, and two real defects hid there until it was pulled out and run.
- A `workflow_dispatch` run builds `N.M.0.0` from `SpecificationVersion`. That version is never
  published: its shape says it is a rehearsal.

**`O.P` start again with every new version of the document.** For document `N.M` the
preliminary releases are `vN.M.0.x` and the first production one is `vN.M.1.0`. After
`v2.1.3.2`, if the document becomes 2.2, the next tag is `v2.2.0.1` or `v2.2.1.0` — never
`v2.2.3.3`. The app's version says how far it has come against *that* document, not how far it
has come overall.

Raising `SpecificationVersion` is part of the change that brings the code in line with a
revised document, not a separate step afterwards.

## The app's words assume a reader who has not read the document

Someone using the app has not read the proposal and should not have to. Every string in the
app is Catalan written on that assumption, and this is a constraint on the writing, not a
matter of taste.

- **No vocabulary from the document.** Not *extra-setmanal*, *cicle setmanal*, *encaix*,
  *assignació*. The names of the days and the blocks are the exception: those are what the
  calendar calls things, and the app is where a reader learns them.
- **No words that suggest something is wrong.** Not *aturar*, *endarrerir*, *desfasament*,
  *error*. A Jarfino does not stop, delay or break anything; it is simply a day that belongs
  to no weekday.
- **Say what a thing is, not how this calendar differs.** "L'any té 52 setmanes justes i els
  dies com aquest queden a part, i així cada mes comença en dilluns, tots els anys" states a
  fact and what follows from it. "És el que endarrereix el compte" asks the reader to hold a
  comparison in their head and hints at a defect in the answer.
- **"Demà" and "ahir" belong to today and to nothing else.** A distance from the day being
  looked at is *l'endemà* or *N dies després*; only a distance from today may be *avui*,
  *demà*, *ahir* or *fa N dies*. The two are different measurements and the words are not
  interchangeable.

Dates shown beside a Solstica date carry their Gregorian year. Around the turn of the year the
two disagree — 1 Unua 2028 is 21 December 2027 — and a reader should not have to work that out.

**No new text enters the app unless it is in all four languages.** Catalan, Spanish, English
and Esperanto live in `Resources/Strings/AppStrings*.resx`, Catalan being the original the
others are translated from. A string added to one file and not the others is a screen that
falls back to Catalan for a reader who does not read it, which is worse than the feature being
absent. The criterion above applies to all four, not only to the one it was written in.

Never translated: the names of the blocks (`Unua`, `Jarmezo`) and of the festivities
(`Jarkomenco`, `Supertago`). Translated: weekdays, the notes beside a block's name, and every
Gregorian date, which takes its shape from the chosen language.

## The proposal is the source of truth

The calendar is specified in a separate document (Plaza Alonso, J., *Solstica Kalendaro*,
<https://doi.org/10.5281/zenodo.22129891>). Where code and document disagree, the document
is right and the code has a bug. Code comments cite the document by section number
(e.g. "section 9.3"); keep that convention when adding code, and cite the section in any
bug report or fix about conversion behaviour.

Do not change the calendar's rules here — the numbers in `ValidityPeriod.Table`, the block
widths and the leap rule are transcriptions of the document, not design decisions open in
this repo.

## Architecture

`src/SolsticaKalendaro.Core` is the executable form of the specification and must stay free
of any MAUI/UI reference so it remains usable from a CLI, web build or test harness.

Two things need per-platform code, for the same reason — MAUI exposes no cross-platform way to
ask or to say: `TextScale`, which reads the system's font size, and `MainActivity.PaintSystemBars`,
which paints the status and navigation bars in the theme's paper with icons to match. Android's
own `colors.xml` (and `values-night/colors.xml`) carry the same two papers and accents, because
the window behind the app and the date picker's dialog are drawn by the system, not by MAUI.

A third: the **icon**. `Resources/AppIcon` holds the dark paper (`appicon.svg`), the figure
(`appiconfg.svg`) and the monochrome variant (`appiconmono.svg`) — the Earth at the December
solstice, its axis leaning away from the Sun, on an orbit of four equal arcs in the dark theme's
season colours. Resizetizer builds the adaptive icon from the first two but points the
monochrome layer at the colour foreground, where the Earth touches the orbit and the Sun carries
a glow; so `Platforms/Android/Resources/mipmap-anydpi-v26/appicon.xml` and `appicon_round.xml`
replace what it generates, naming `drawable/appiconmono.xml` — the same figure hand-carried into
a vector drawable, with the arcs parted and the Sun smaller, since a themed icon is one colour
and shapes that touch become one shape. The splash screen is the same figure on the same paper.

`src/SolsticaKalendaro.App` is the Android app, all of it under one `NavigationPage`: the year
view, the detail of one day, the "Go to" panel — a year typed, a period chosen, or a day named
in either calendar — Options, About, the period, and the introduction. Everything is pushed on top of the year rather than replacing
it, so coming back finds it where the reader left it. A year asked for by name is the exception:
it opens at its top, and a day asked for opens centred and **marked** (`IsMarked` on the cell or
the band, an outline because today is a fill and one day can be both). The mark survives
scrolling, the day detail and Today; it goes whenever `ShowYear` builds the rows again.
`ChoiceRow` draws one line of a list to choose from and `YearField` a year typed into one, and
both are shared, so the second list — or field — a reader meets reads like the first.
`IntroPage` is the way in for a reader who has not read the proposal: what the calendar is, how
to read the year, and what the names mean. It is pushed **modally** over the year view on the
first launch only — `Introduction.Seen`, a preference that skipping, finishing and the system's
back button all set — and afterwards it is there to be asked for, from "How to read it" on the
menu and from About. A page rebuilt for a change of language or of start is not a first launch
and does not show it again. Its middle screen draws a real week and a real Jarfino with the year
view's own row templates, which is why those live in `Resources/Styles/Rows.xaml` rather than in
`MainPage.xaml`: an example that explains the view by being the view cannot afford to drift from
it. A template in a shared dictionary cannot name the page it came from, so the day's tap asks
for `MainPage` by type and finds nothing inside the introduction, which is what an example wants.

`AboutPage` says what the app is: its version (marked preliminary by `ReleaseVersion`, read
from `AppInfo.Current.VersionString`), where new versions and the source are, which version of
the proposal it implements, and the licences. It shows no signing fingerprint — an app printing
its own verifies nothing, and the check belongs before installing, against the README. `Links`
holds the repository and releases URLs; the article's own stays in the core beside the version
it belongs to, and is not copied.

`PeriodPage` says one row of section 9.3 in sentences — the seasons' days, the blocks' widths,
and what changed when the period began — with arrows that walk the table on the page itself and
never move the year behind it. It is reached from the period line in the header and from the
foot of the day detail, and it is where the app names the document it implements and links to it.

**The app follows the system's theme and has no setting of its own either.** `Palette` holds
every colour as a light/dark pair and writes the active half into the application's resources
under the names the rest of the app already used (`Paper`, `Ink`, …), so nothing else knows
which theme is in force. The XAML asks for them with **`DynamicResource`**, so a page on screen
follows at once; the views built in code read their colour once, so `Application.RequestedThemeChanged`
applies the palette and calls `Rebuild.WhereTheReaderStands()` — the same thing a change of
language, of start date or of text size does. No colour is written anywhere else: a hex outside
`Palette` is a colour that cannot have a dark half. The light `Faint` was darkened to `#6E665A`
because the small Gregorian dates reached only 3.95:1 on the paper and 3.42:1 on the day-off
shade; it is 5.29:1 and 4.57:1 now.

**The app follows the system's font size and has no setting of its own.** `TextScale` reads it
per platform, as above, because MAUI exposes no cross-platform way to ask, and an app cannot
follow what it cannot read. Text screens simply grow and scroll. The
year view cannot: seven columns would be wider than the screen, so `TextScale.Grid` stops at
160 %, and above 130 % (`GridShowsMonth`) a cell's Gregorian line is the day alone — the month
is in the day detail. The grid's row heights and font sizes are application resources scaled
once, before any page is built, and the heights stay whole numbers: **every row declares one and
scrolling to a day by index depends on it**, so the grid sets `FontAutoScalingEnabled="False"`
everywhere and scales itself. The setting can change while the app is in the background, and
row heights cannot change under a live page, so `MainPage` rebuilds the pages on resume when
`TextScale.HasChanged`, keeping the reader's place as a change of language does.

**One marking for every day off, whatever the reason.** A rest day, a festivity of the calendar
and later a local holiday (#25) all look the same in the grid: the cell shaded `DayOffShade` and
the number `DayOffRed` and semibold, the bands the same with their name in red, and the rest
days' whole columns — their heads included, which is why `WeekdayHeader` is built in code and
shared with the introduction's example. Which reason it is belongs to the day detail, which says
one line per reason; telling them apart in the grid would take a legend. `RestDays` holds the
reader's choice (Saturday and Sunday by default, none allowed), as `Language` and
`CalendarStart` hold theirs, and `YearView` asks `IsDayOff` once per day while building the rows.

Because red and shading now mean "day off", **today is an ink circle behind the number** — a day
can be both — and the go-to mark is violet (`Mark`), not red. All of it is drawn inside the rows'
fixed heights: 44, 58 and 66 are what `ScrollTo` by index depends on.

It holds no calendar arithmetic of its own — every date, weekday and season it shows comes
from `Core`. Keep it that way; a rule reimplemented in the UI is a rule that can disagree
with the document. `YearView` projects an outline into bindable rows and `Text` holds the
Catalan, which is the whole of the app's own logic: which Gregorian month to repeat, what to
call a block, how to say "demà". The core writes no sentences, so all of that belongs here.

Its csproj blanks the `TargetFramework` inherited from `Directory.Build.props`, which would
otherwise win over `TargetFrameworks` and silently build it as plain `net10.0`.

The conversion is built in four layers, each answering one question:

- **`SolsticaEpoch`** — *which Gregorian date is 1 Unua?* One choice fixes the whole mapping
  forever, because both calendars use the 4/100/400 rule with the same year numbering.
  `Expository2026` (21 Dec 2026 = 1 Unua 2027) is the app's default; `AdoptionWindows` holds the
  section 7.6 candidates, and the app offers all of them. **Two epochs anchored on the same day
  of December are the same calendar wherever they overlap**, so switching between them only
  removes or adds the years before the first one. The 2031 window (1 Unua 2032) anchors on 22
  December (`IsOffAnchor`) and is the only choice that moves a date: one day later, for ever.
  `EpochChoiceTests` states both. In the app the choice lives in `CalendarStart`, which is the
  only place that may build one — and the app's own words for it are "when the calendar begins",
  never "epoch" or "adoption window".
- **`ValidityPeriod`** — *what are the rules in this year?* Eleven rows covering 2000–10000
  (section 9.3), each carrying a `SeasonAllocation`, a `BlockWidths` arrangement and the
  `SupertagoSeam`. Resolved by year via `ValidityPeriod.For`. Outside the table, conversion
  throws. A row also knows its neighbours (`Previous`, `Next`), the day the Supertago follows
  (`SupertagoFollows`, the seam named as a block and a day) and `ChangesOnEntering`: the seasons
  that change length, the blocks that change width, the months that move and where the Supertago
  goes, all compared against the row before and none of it written down. It is null for the
  first period, which begins nothing.
- **`YearLayout`** — *where does each block sit?* Start ordinal and length per block for a
  given `BlockWidths`, cached per arrangement. Ordinals here are **common-year ordinals**:
  the Supertago is not accounted for at this layer.
- **`SolsticaCalendar`** — the conversion itself, plus `WeekDay`, `SeasonOf`, `YearFraction`,
  `IsDayOff(date, restDays)` — a festivity, or a day of the week the reader rests on, with the
  extra-weekly days never consulting the set because they have no weekday — and `Blocks(year)`: the blocks of a year in the order they occur, with the days in each.
  The Supertago is one of them in a leap year, placed by the day of the year it opens on, which
  is where the period puts it — inside a block in two of the eleven periods, between two in the
  rest. An interface offering a day to pick has to offer exactly these.

`YearOutline.cs` sits on top of those four. `SolsticaCalendar.Outline(year)` returns a year
as the rows an interface paints from top to bottom: a `SectionHeader` per month and
transition block, `WeekRow`s of seven days Monday to Sunday, and an `ExtraWeeklyRow` for the
Jarfino and the Supertago — which open no section, and whose position comes from the
validity period. Every row carries whole `OutlineDay`s: the Gregorian date (null where
`CanConvert` says there is none), the season, and whether the day is a festivity, so the UI
never has to ask the calendar a second question.

**No formatting belongs in the outline.** When to repeat the Gregorian month, how to name a
weekday, what to abbreviate: that depends on the culture and on the space available, and it
stays in the interface.

`SolsticaCalendar.Describe(date)` gathers a single day for a detail screen, `LastShift` and
`NextShift` included: those name the extra-weekly days that account for the gap between the
two weekdays, so a screen can point at the cause rather than derive it.

**`UpcomingFestivities` is strictly forward-looking.** A festivity falling on the date asked
about is never in the list — `from` is a position, not a range. A screen showing a day must
therefore surface that day's own festivity separately (`SolsticaDate.FestivityName` says
whether it has one) instead of expecting it at the head of the list. Today being the
Supertago is exactly when the reader most wants to be told so.

All arithmetic goes through `DateOnly.DayNumber` (exact integer day counts). Never introduce
a floating-point Julian Day: exactness is a property the tests rely on.

### Invariants that are easy to break

- **Common-year ordinal vs. day-of-year.** `YearLayout.CommonOrdinal` / `FromCommonOrdinal`
  ignore the Supertago; `SolsticaCalendar.DayOfYear` / `FromDayOfYear` insert it. Mixing the
  two silently shifts leap-year dates by one day.
- **Period-awareness is narrow.** Recalibration is *semantic*: it moves no date in a common
  year (section 9.4). Only the block arrangement (one change, in 3324) affects common years;
  the `SupertagoSeam` affects leap years only, and only the days between the old and new seams.
- **Two dates, two different facts.** From the 7722 period the integer allocation of §9.3
  saturates: s1 = 91 = 84 + w1, so the 0° season boundary sits on the Ekvinokso I's closing
  seam rather than inside it. `ContainmentEndsYear` = 8537 is when the astronomical 0°
  point itself leaves the block (§9.6). Between them the real equinox still falls inside the
  Ekvinokso I; only the rounding to whole days puts the boundary on the edge. Neither is a
  bug, and neither should be "reconciled" in code.
- **Extra-weekly days pause the seven-day cycle**, they do not belong to it. `WeekDay` returns
  `null` for Jarfino and Supertago, and the Solstica weekday diverges from the Gregorian
  weekday of the same physical day after the first Jarfino. That divergence is the design's
  trade-off, not a defect — the app shows and labels both.
- **Every block starts on a Monday**, in every period, because every block length is a
  multiple of seven. `BlockWidths.IsWellFormed` and the tests enforce this.
- **Derive, don't tabulate.** The Rekomenco is computed from its astronomical rule
  (`RekomencoCommonOrdinal`) rather than hard-coded, so it follows the period. Prefer the same
  approach for anything else the document defines by rule.

### Tests

`tests/SolsticaKalendaro.Core.Tests/SolsticaCalendarTests.cs` (xunit) is a property suite over
the whole tabulated window, not a handful of examples: every day round-trips, consecutive
Gregorian days map to consecutive Solstica days, every block starts on a Monday in every
period, the table tiles 2000–10000 without gap or overlap. New conversion code should be
covered the same way — assert the invariant across all periods rather than spot-checking dates.

## Naming

Domain names come from the proposal and are Esperanto (`Unua`, `Jarmezo`, `Supertago`,
`Ekvinokso I`, `Naŭa`). `.editorconfig` disables CA1707 so the analyser will not "fix" them.
Keep the document's names; `PeriodKindExtensions.Name()` handles the display spellings
(`Naŭa`, `Dek-unua`, `Ekvinokso I`).
