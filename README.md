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

That DOI is the concept one: it always resolves to the newest version of the document.
**This app implements version 2.1**, which is
<https://doi.org/10.5281/zenodo.22755480>. The two are not interchangeable — anyone
checking the app against the proposal needs the version it was built against, not
whichever is newest — and the first half of every release tag says which that is.

The version and its link live together in the core, as `SolsticaCalendar.SpecificationVersion`
and `SolsticaCalendar.SpecificationUrl`, and the app's period screen prints both: it says which
version it follows and offers the link to it.

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

## Installing it

The app is not on any store, so each release carries the APK itself. It needs Android 5.0
or newer.

1. Open [Releases](https://github.com/j-plaza1/solstica-kalendaro-app/releases) and
   download `solstica-kalendaro-N.M.O.P.apk`.
2. Open the downloaded file. Android will stop and offer a setting the first time: allow
   installing apps from whichever app you downloaded with. That prompt is Android asking
   whether you trust the source, not a sign that something is wrong — but it is worth
   turning the permission back off afterwards.

The app installs as `io.github.j_plaza1.solsticakalendaro`.

### Checking what you downloaded

Every release is signed with the same key, which is why Android will refuse an "update"
signed by anybody else. To check a file before installing it:

```bash
apksigner verify --print-certs solstica-kalendaro-N.M.O.P.apk
```

The certificate is `CN=Javier Plaza Alonso, C=ES`, and its SHA-256 fingerprint is:

```
ac76ef7e5a6e65d35c32284119de5891715c33f135d8808bbbd55a4a34f63476
```

The same fingerprint, grouped in bytes, which is how `keytool` and Android's own settings
screens print it:

```
AC:76:EF:7E:5A:6E:65:D3:5C:32:28:41:19:DE:58:91:71:5C:33:F1:35:D8:80:8B:BB:D5:5A:4A:34:F6:34:76
```

The two are the same 32 bytes written two ways; which one you see depends on the tool. The
release workflow prints both packages' fingerprint in its *Verify the signatures* step, and
compares each of them against the one written here: a build signed with another key fails
there rather than being published.

### The bundle for Google Play

The same publish produces a second package, an **Android App Bundle** (`.aab`). Play accepts
nothing else, and building it beside the APK keeps the two one version, one internal version
number and one signing key.

It is not attached to the release. An AAB is not installable — it is what Google Play builds
the installable APKs from — so a file sitting beside the APK on a release page would only be
downloaded by someone who then could not install it. Instead each tag run keeps it as an
artifact of the run itself, `solstica-kalendaro-aab-N.M.O.P`, and it is uploaded to Play
Console by hand from there. A rehearsal run keeps one too, named `…-assaig`, which is how to
tell at a glance that it is not the one to upload.

## What the version numbers mean

A release is `N.M.O.P`. The first half is the version of the proposal document the app
implements — the app and the document are versioned together, so `2.1.x.y` is an app built
against version 2.1 of the proposal. The second half is the app's own version, and it starts
again whenever the document is revised.

An `O` of zero means preliminary: `2.1.0.1` is an early build against document 2.1, and
`2.1.1.0` is the first one meant for daily use. GitHub marks the preliminary ones as
pre-releases, and so does the app: its About screen reads the same rule from
`ReleaseVersion.IsPreview` and says "(preview)" beside the version.

## The icon

The Earth at the December solstice, where the year begins: its axis leaning away from the Sun —
that lean is what makes it a solstice rather than a picture of a planet — on an orbit drawn as
four equal arcs in the season colours, because each season is 90° of ecliptic longitude and the
seasons differ in days only because the Earth moves at different speeds. There is a monochrome
variant for Android's themed icons, with the arcs parted and the Sun smaller so that nothing
touches when there is no colour to tell the shapes apart, and the splash screen is the same
figure on the same dark paper.

The rules behind those numbers live in the core, in `ReleaseVersion`, and
`tools/ReleaseVersion` is a small console tool over them:

```bash
dotnet run --project tools/ReleaseVersion -c Release -- tag v2.1.1.0
```

The release workflow asks it what version it is building, and CI runs it on every pull request
— valid tags, a rehearsal and tags that must be refused — so a mistake in it is a red pull
request rather than a surprise at the moment of publishing.

## Repository layout

```
src/SolsticaKalendaro.Core        conversion library — no UI dependencies
src/SolsticaKalendaro.App         the Android app: .NET MAUI, year view, day detail, panel,
                                  period, options, about and the introduction
tests/SolsticaKalendaro.Core.Tests
tools/ReleaseVersion               what version a release run is building
```

`SolsticaKalendaro.Core` is deliberately free of any MAUI reference. It is the executable
form of the specification, and it should stay usable from a CLI, a web build or a test
harness without dragging a UI framework along.

## Design notes worth knowing before reading the code

**The reader chooses when the calendar begins.** The correspondence between the two calendars
is fixed by one choice: which Gregorian date is 1 Unua. The app offers 21 December 2026 — the
expository epoch of the document, and the default — and the adoption windows of section 7.6,
the years in which the December solstice falls on a Monday. The option list says none of that
in those words: it asks when the calendar begins and lists the dates.

**Choosing changes less than it looks.** Because both calendars use the 4/100/400 rule with the
same year numbering, an epoch fixes the mapping for ever, and two epochs anchored on the same
day of December are the same calendar wherever they overlap. Every option but one anchors on 21
December, so switching between them only removes or adds the years before the first one. The
2031 window anchors on 22 December, and that is the only choice that moves a date: every
Gregorian date one day later, for ever. `EpochChoiceTests` states both halves.

**So is light or dark.** Every colour is a light/dark pair, chosen together — the seasonal
stripe is read by comparing four colours with each other — and the app writes the active half
of each pair into its resources at startup and again whenever the system's theme changes,
rebuilding the pages where the reader stands. The dark values meet 4.5:1 for text and 3:1 for
the stripes on both the dark paper and the day-off shade; the light `Faint` was darkened for
the same reason, since the small Gregorian dates under each day did not reach 4.5:1 on either.

**Large text is the system's business, not the app's.** The app follows the font size already
set on the device. The text screens grow without limit and scroll; the year view grows with
them up to 160 %, because seven columns cannot get wider than the screen, and past 130 % a
cell shows the day of the Gregorian month without its name. The rows keep fixed heights —
scrolled to by index, which is how Today and Go to land on the exact day — so they are scaled
once at launch rather than measured per row.

**Rest days are days of the Solstica week.** Once the calendar is adopted everything lives
in it, so the days a reader rests on are Monday-to-Sunday in this calendar and not in the
Gregorian one — and after the first Jarfino those are different physical days, which is the
whole practical consequence of the proposal. The reader chooses them (Saturday and Sunday by
default); `SolsticaCalendar.IsDayOff` answers whether a day is free, counting the calendar's own
festivities, and the grid marks every free day the same way whatever makes it one. The reasons
are told apart on the day itself, not in the grid.

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
