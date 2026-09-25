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

CI (`.github/workflows/ci.yml`) runs restore / build / test in Release on push to `main`
and on PRs, scoped to the test project. The Android job is still commented out.

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

- `ApplicationDisplayVersion` is `N.M.O.P`. `ApplicationVersion` is
  `N*1000000 + M*10000 + O*100 + P`, so no component may exceed 99; the workflow rejects one
  that does.
- `O = 0` means preliminary. Those releases are marked prerelease, and the first production
  release against a document is `vN.M.1.0`.
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

`src/SolsticaKalendaro.App` is the Android app: two pages under a `NavigationPage`, the year
view and the detail of one day. The year is pushed under the detail rather than replaced, so
coming back finds it where the reader left it.

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
  Defaults to `Expository2026` (21 Dec 2026 = 1 Unua 2027); `AdoptionWindows` holds the
  section 7.6 candidates. The 2031 adoption window (1 Unua 2032) anchors on 22 December (`IsOffAnchor`), which
  shifts its correspondence permanently one day — surface that in any epoch picker.
- **`ValidityPeriod`** — *what are the rules in this year?* Eleven rows covering 2000–10000
  (section 9.3), each carrying a `SeasonAllocation`, a `BlockWidths` arrangement and the
  `SupertagoSeam`. Resolved by year via `ValidityPeriod.For`. Outside the table, conversion
  throws.
- **`YearLayout`** — *where does each block sit?* Start ordinal and length per block for a
  given `BlockWidths`, cached per arrangement. Ordinals here are **common-year ordinals**:
  the Supertago is not accounted for at this layer.
- **`SolsticaCalendar`** — the conversion itself, plus `WeekDay`, `SeasonOf`, `YearFraction`.

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
