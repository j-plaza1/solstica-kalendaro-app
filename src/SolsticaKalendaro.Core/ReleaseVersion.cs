using System.Globalization;

namespace SolsticaKalendaro.Core;

/// <summary>
/// What a release is called: <c>N.M.O.P</c>, where <c>N.M</c> is the version of the proposal the
/// app implements — the core states it in <see cref="SolsticaCalendar.SpecificationVersion"/> —
/// and <c>O.P</c> is the app's own, starting again with every revision of the document.
///
/// <see cref="Of"/> is what the release workflow used to do in a shell block, which ran only
/// while publishing and where two real defects hid until the block was pulled out and run. It
/// is here because this is where the document's version already is: the check no longer reads
/// it out of the source with a pattern, so it cannot end up measuring a tag against nonsense.
/// </summary>
public static class ReleaseVersion
{
    /// <summary>What a version looks like once it has been read.</summary>
    /// <param name="Display">The four numbers as written, without the tag's "v".</param>
    /// <param name="Code">
    /// The internal number, <c>N*1000000 + M*10000 + O*100 + P</c>. It only ever grows and can
    /// be worked out again from the tag, rather than being a counter someone has to remember.
    /// </param>
    /// <param name="Prerelease">True while <c>O</c> is 0: preliminary, not for daily use.</param>
    public sealed record Release(string Display, int Code, bool Prerelease);

    /// <summary>
    /// The version a run is building, or why it is not a version. Bad input is an answer, not
    /// an exception: this is called from a workflow, which wants a message to print.
    /// </summary>
    /// <param name="refType">"tag" for a tag; anything else is a rehearsal (workflow_dispatch).</param>
    /// <param name="refName">The tag, "vN.M.O.P", or the branch a rehearsal runs from.</param>
    /// <param name="specification">
    /// The document version the app implements. Defaults to the core's own, which is the point
    /// of putting this here.
    /// </param>
    public static (Release? Release, string? Error) Of(string? refType, string? refName,
                                                       string? specification = null)
    {
        specification ??= SolsticaCalendar.SpecificationVersion;

        var (documentN, documentM, documentError) = Document(specification);
        if (documentError is not null) return (null, documentError);

        // A rehearsal builds N.M.0.0: a version that is never published, and that says so by
        // its shape.
        string version = refType == "tag"
            ? (refName ?? string.Empty)
            : $"{specification}.0.0";

        if (refType == "tag")
        {
            if (!version.StartsWith('v'))
                return (null, $"Tag \"{version}\" is not vN.M.O.P: it does not begin with \"v\".");

            version = version[1..];
        }

        if (Components(version) is not { } parts)
            return (null, $"Tag \"{refName}\" is not vN.M.O.P: it has to be four whole numbers "
                          + "from 0 to 99, separated by dots.");

        // The app is released against a stated version of the document, so a tag that names a
        // different one is a mistake worth stopping for rather than shipping.
        if (parts[0] != documentN || parts[1] != documentM)
            return (null, $"Tag \"{refName}\" says document {parts[0]}.{parts[1]}, "
                          + $"but the core implements {specification}.");

        int code = (parts[0] * 1000000) + (parts[1] * 10000) + (parts[2] * 100) + parts[3];
        return (new Release(version, code, parts[2] == 0), null);
    }

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

    /// <summary>
    /// The document's two numbers, or why they are not two numbers. A check that cannot read
    /// the value it checks against must not pass.
    /// </summary>
    private static (int N, int M, string? Error) Document(string specification)
    {
        string[] parts = specification.Split('.');

        if (parts.Length != 2)
            return (0, 0, $"SpecificationVersion is \"{specification}\"; it has to be N.M, "
                          + "two components and no more.");

        if (Number(parts[0]) is not { } n || Number(parts[1]) is not { } m)
            return (0, 0, $"SpecificationVersion is \"{specification}\": it has to be N.M with "
                          + "whole numbers from 0 to 99.");

        return (n, m, null);
    }

    /// <summary>The four numbers, or null for anything that is not four of them.</summary>
    private static int[]? Components(string? version)
    {
        if (version is null) return null;

        string[] parts = version.Split('.');
        if (parts.Length != 4) return null;

        var numbers = new int[4];
        for (int i = 0; i < 4; i++)
        {
            if (Number(parts[i]) is not { } value) return null;
            numbers[i] = value;
        }

        return numbers;
    }

    /// <summary>
    /// One component: digits only, and no more than two of them in value. Both halves of a
    /// version are packed into two digits of the internal number, so both obey the same rule.
    /// </summary>
    private static int? Number(string part) =>
        int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
        && value is >= 0 and <= 99
            ? value
            : null;
}
