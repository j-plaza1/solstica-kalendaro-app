using Xunit;

namespace SolsticaKalendaro.Core.Tests;

/// <summary>
/// Which builds call themselves preliminary. The rule is the release convention's own — an O of
/// zero — and the About screen and the release workflow have to read a version the same way.
/// </summary>
public class ReleaseVersionTests
{
    [Theory]
    [InlineData("2.1.0.1")]     // a preliminary build against document 2.1
    [InlineData("2.1.0.0")]     // what a workflow_dispatch rehearsal builds
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
