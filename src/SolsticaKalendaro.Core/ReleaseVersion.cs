using System.Globalization;

namespace SolsticaKalendaro.Core;

/// <summary>
/// The shape of a release version, <c>N.M.O.P</c>: <c>N.M</c> is the version of the proposal the
/// app implements — the core states it in <see cref="SolsticaCalendar.SpecificationVersion"/> —
/// and <c>O.P</c> is the app's own, starting again with every revision of the document.
///
/// The convention lives here because its first half does, and because a screen that marks a
/// build as preliminary should be asking the same question the release workflow asks.
/// </summary>
public static class ReleaseVersion
{
    /// <summary>
    /// Whether this is a preliminary release: <c>O</c> of zero, as in 2.1.0.1. The first release
    /// meant for daily use against a document is <c>N.M.1.0</c>.
    ///
    /// Anything that is not four numbers is not a release version at all, and gets no answer of
    /// its own rather than an exception: a screen showing it is reporting what it was built
    /// with, and a build from a working copy is not a release.
    /// </summary>
    public static bool IsPreview(string? version) =>
        Components(version) is { } parts && parts[2] == 0;

    /// <summary>The four numbers, or null for anything that is not four plain numbers.</summary>
    private static int[]? Components(string? version)
    {
        if (version is null) return null;

        string[] parts = version.Split('.');
        if (parts.Length != 4) return null;

        var numbers = new int[4];
        for (int i = 0; i < 4; i++)
            if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
                return null;

        return numbers;
    }
}
