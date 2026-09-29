using Xunit;

namespace SolsticaKalendaro.Core.Tests;

/// <summary>
/// Which builds call themselves preliminary, and what version a release run is building. The
/// second was a shell block that ran only while publishing, where two real defects hid until it
/// was pulled out and run; here it is exercised on every pull request, and a tag that no
/// release has taken yet is taken before the first one takes it.
/// </summary>
public class ReleaseVersionTests
{
    // ---------- what a run builds ----------

    [Fact]
    public void AProductionTagIsNotAPrerelease()
    {
        var (release, error) = ReleaseVersion.Of("tag", "v2.1.1.0");

        Assert.Null(error);
        Assert.Equal("2.1.1.0", release!.Display);
        Assert.Equal(2010100, release.Code);
        Assert.False(release.Prerelease);
    }

    [Fact]
    public void ATagWithAThirdComponentOfZeroIsAPrerelease()
    {
        var (release, error) = ReleaseVersion.Of("tag", "v2.1.0.1");

        Assert.Null(error);
        Assert.Equal("2.1.0.1", release!.Display);
        Assert.Equal(2010001, release.Code);
        Assert.True(release.Prerelease);
    }

    [Fact]
    public void EachComponentKeepsItsOwnTwoDigitsOfTheCode()
    {
        var (release, error) = ReleaseVersion.Of("tag", "v2.1.12.34");

        Assert.Null(error);
        Assert.Equal(2011234, release!.Code);
    }

    [Fact]
    public void ARunThatIsNotATagIsARehearsalOfTheDocumentsOwnVersion()
    {
        // A dispatch run builds N.M.0.0: never published, and saying so by its shape.
        var (release, error) = ReleaseVersion.Of("branch", "main");

        Assert.Null(error);
        Assert.Equal("2.1.0.0", release!.Display);
        Assert.Equal(2010000, release.Code);
        Assert.True(release.Prerelease);
    }

    [Fact]
    public void TheDocumentVersionComesFromTheCoreUnlessItIsGiven()
    {
        var (fromTheCore, _) = ReleaseVersion.Of("branch", "main");
        var (given, _) = ReleaseVersion.Of("branch", "main", SolsticaCalendar.SpecificationVersion);

        Assert.Equal(given!.Display, fromTheCore!.Display);
        Assert.StartsWith(SolsticaCalendar.SpecificationVersion + ".", fromTheCore.Display);
    }

    // ---------- what is not a version ----------

    [Theory]
    [InlineData("v2.1.1")]          // three components
    [InlineData("v2.1.1.0.0")]      // five
    [InlineData("v2.1.100.0")]      // over 99, which the internal number has no room for
    [InlineData("v2.1.-1.0")]
    [InlineData("v2.1.x.0")]
    [InlineData("2.1.1.0")]         // no "v"
    [InlineData("v2.2.1.0")]        // a document this core does not implement
    [InlineData("")]
    [InlineData(null)]
    public void ATagOfAnyOtherShapeIsRefusedWithAReason(string? tag)
    {
        var (release, error) = ReleaseVersion.Of("tag", tag);

        Assert.Null(release);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("2")]
    [InlineData("2.1.0")]
    [InlineData("2.x")]
    public void AMalformedDocumentVersionStopsTheRunRatherThanWavingItThrough(string specification)
    {
        // The defect this guards against: a check that cannot read the value it checks against
        // used to compare every tag with an empty string and accept all of them.
        var (release, error) = ReleaseVersion.Of("tag", "v2.1.1.0", specification);

        Assert.Null(release);
        Assert.Contains("SpecificationVersion", error);
    }

    // ---------- what the About screen asks ----------

    [Theory]
    [InlineData("2.1.0.1")]     // a preliminary build against document 2.1
    [InlineData("2.1.0.0")]     // what a rehearsal builds
    [InlineData("10.4.0.12")]
    public void AThirdComponentOfZeroMeansPreliminary(string version) =>
        Assert.True(ReleaseVersion.IsPreview(version));

    [Theory]
    [InlineData("2.1.1.0")]     // the first release meant for daily use
    [InlineData("2.1.12.3")]
    [InlineData("2.2.1.0")]
    public void AnythingElseIsARelease(string version) =>
        Assert.False(ReleaseVersion.IsPreview(version));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2.1.0")]       // three components: not this convention
    [InlineData("2.1.0.1.4")]
    [InlineData("2.1.x.1")]
    [InlineData("2.1.-0.1")]
    [InlineData("not a version")]
    public void AVersionOfAnotherShapeIsNotMarkedAndDoesNotThrow(string? version) =>
        Assert.False(ReleaseVersion.IsPreview(version));
}
