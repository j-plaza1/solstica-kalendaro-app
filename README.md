# Solstica Kalendaro

A dual-calendar app for Android: today's date shown simultaneously in the Gregorian
calendar and in the *Solstica Kalendaro*, a proposed perpetual solar calendar anchored
at the December solstice and calibrated to the real lengths of the seasons.

The calendar itself is specified in a separate document. **This repository is the
application; the proposal is the source of truth.** Where the two disagree, the document
is right and the code has a bug.

> Plaza Alonso, J. *Solstica Kalendaro: a proposal for a regular, solstice-anchored
> calendar faithful to the real lengths of the seasons.*
> <https://doi.org/10.5281/zenodo.22129891> (CC BY 4.0)

## What the calendar looks like

A year is twelve months of 28 days each, separated into four seasons of three months,
with three transition blocks totalling 28 days placed at the equinoxes and the June
solstice, and one extra-weekly day closing the year:

```
Unua Dua Tria | Ekvinokso I | Kvara Kvina Sesa | Jarmezo |
Sepa Oka Naŭa | Ekvinokso II | Deka Dek-unua Dek-dua | Jarfino
```

Every month is an identical 4 × 7 grid, and every month begins on a Monday. Leap years
add the *Supertago*, a second extra-weekly day.

**One thing that surprises people, and is not a bug.** The two extra-weekly days pause
the seven-day cycle rather than belonging to it. That is what makes the calendar
perpetual, and it means the Solstica weekday drifts away from the Gregorian weekday of
the same physical day after the first *Jarfino*. The app shows both and labels them.

## Repository layout

```
src/SolsticaKalendaro.Core        conversion library — no UI dependencies
src/SolsticaKalendaro.App         the Android app: .NET MAUI, one page
tests/SolsticaKalendaro.Core.Tests
```

`SolsticaKalendaro.Core` is deliberately free of any MAUI reference. It is the executable
form of the specification, and it should stay usable from a CLI, a web build or a test
harness without dragging a UI framework along.

## Design notes worth knowing before reading the code

**The epoch is configurable.** The correspondence between the two calendars is fixed by
one choice: which Gregorian date is 1 Unua. The app defaults to 21 December 2026 = 1 Unua
2027, the expository epoch of the document, and offers the adoption windows of section 7.6
— the years in which the December solstice falls on a Monday. Because both calendars use
the 4/100/400 rule with the same year numbering, once that choice is made the two stay in
permanent lockstep and 1 Unua always falls on the same Gregorian date.

**Conversion is period-aware, but only for leap years.** Section 9 of the document
recalibrates the seasonal allocation eleven times between 2000 and 10000, and moves the
*Supertago* to a different seam when the deficient season changes. Reallocation moves no
date in a common year. `ValidityPeriod` carries the table; `SolsticaCalendar` resolves it
from the year.

**One structural reform, in 3324.** The transition blocks go from 7/14/7 to 7/7/14, which
moves *Sepa*, *Oka* and *Naŭa* seven days earlier and leaves the rest of the year where it
was. It is the only such change in the whole tabulated window.

## Getting started

Requires the .NET 10 SDK.

```bash
dotnet test tests/SolsticaKalendaro.Core.Tests    # the core library and its test suite
```

The app targets Android only for now, and needs the MAUI Android workload:

```bash
dotnet workload install maui-android
dotnet build src/SolsticaKalendaro.App -f net10.0-android -t:Run   # deploy to a running device
```

**Clone to a path with no non-ASCII characters.** Android's `aapt2` refuses to compile
resources under one and fails with `APT2265`, which reads as a resource error rather than
a path error. Accents, emoji and the like in any parent folder are enough. The core
library and its tests do not care; only the Android build does.

## Typefaces

The app sets its display type in **Spectral** and everything else in **IBM Plex Sans**,
both under the SIL Open Font License 1.1. The faces live in
`src/SolsticaKalendaro.App/Resources/Fonts` with each family's licence beside them, and
each weight is registered under its own alias: MAUI's `FontAttributes` knows only Bold, so
a SemiBold or Medium face has to be named directly.

## Contributing

Bug reports about the *calendar* — a date that converts wrongly, a weekday that does not
match — are the most useful kind, and should cite the section of the document they
contradict. Proposals to change the calendar itself belong with the document, not here.

## Licence

The code is under the MIT licence (see `LICENSE`). The proposal document is separately
licensed CC BY 4.0 by its author.
