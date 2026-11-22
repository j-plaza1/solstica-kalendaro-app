#!/usr/bin/env python3
"""How much of the DEX code R8 covered, and whether that is enough for Google Play.

From February 2027 a bundle on Play has to be at least 25 % optimized, 25 % shrunk and
25 % obfuscated. R8 writes what it achieved into r8.json: in obj/ beside the DEX while
building, and inside the bundle at BUNDLE-METADATA/com.android.tools/r8.json, which is
where Play reads it. Both are the same file, so this reads either.

Takes the path to one r8.json; prints the three figures and exits non-zero if any of them
is short, naming which and by how much. It is the one statement of the rule: CI runs it on
every pull request, and the release runs it on the bundle it is about to keep for Play.
"""

import json
import sys

REQUIRED = 25.0

# R8 reports the share it did NOT do, so each figure here is the complement: Play asks for
# 25 % done, which is at most 75 % left.
FIGURES = [
    ("optimization", "noOptimizationPercentage"),
    ("shrinking", "noShrinkingPercentage"),
    ("obfuscation", "noObfuscationPercentage"),
]


def main(path: str) -> int:
    try:
        with open(path, encoding="utf-8") as f:
            stats = json.load(f)["stats"]
    except FileNotFoundError:
        print(f"::error::{path} is missing, so R8 did not run. "
              f"AndroidLinkTool=r8 is what turns it on.")
        return 1
    except (ValueError, KeyError) as why:
        print(f"::error::{path} is not R8 metadata this can read: {why}")
        return 1

    short = False
    for what, reported in FIGURES:
        # A figure the file does not carry ends the run rather than passing as a zero.
        if reported not in stats:
            print(f"::error::The R8 metadata carries no {reported}, so {what} cannot be checked.")
            short = True
            continue

        done = 100.0 - float(stats[reported])
        if done < REQUIRED:
            print(f"::error::R8 did too little {what}: {done:.2f} %, "
                  f"and Google Play asks for {REQUIRED:.0f} %.")
            short = True
        else:
            print(f"{what}: {done:.2f} %")

    return 1 if short else 0


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print(f"usage: {sys.argv[0]} <path to r8.json>", file=sys.stderr)
        sys.exit(2)
    sys.exit(main(sys.argv[1]))
