using System.Globalization;
using SolsticaKalendaro.Core;

// What version a release run is building, asked by release.yml and by CI on every pull
// request. The rules are the core's — beside the version of the document they are checked
// against — and this only says the answer in the shape a workflow can read:
//
//     dotnet run --project tools/ReleaseVersion -c Release -- <ref_type> <ref_name>
//
// Three lines on standard output and 0 when there is a version; ::error:: on standard error
// and a non-zero code when there is not, so a workflow stops rather than publishing nonsense.

if (args.Length != 2)
{
    Console.Error.WriteLine("::error::Usage: releaseversion <ref_type> <ref_name>. "
                            + "ref_type is \"tag\" for a tag and anything else for a rehearsal.");
    return 2;
}

var (release, error) = ReleaseVersion.Of(args[0], args[1]);

if (release is null)
{
    Console.Error.WriteLine($"::error::{error}");
    return 1;
}

Console.WriteLine($"display={release.Display}");
Console.WriteLine($"code={release.Code.ToString(CultureInfo.InvariantCulture)}");
Console.WriteLine($"prerelease={(release.Prerelease ? "true" : "false")}");
return 0;
