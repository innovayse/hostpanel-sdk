using Hostpanel.Sdk.Packaging.Manifest;
using Hostpanel.Sdk.Packaging.Naming;

namespace Hostpanel.Sdk.Packaging.Tests.Manifest;

/// <summary>Tests of <see cref="PluginSdkHostReleases"/>: which host release first ships an SDK a plugin was built on.</summary>
public sealed class PluginSdkHostReleasesTests
{
    /// <summary>The first host release shipping an SDK at least as new as the plugin's is the answer, including the releases between two rows.</summary>
    /// <param name="sdk">The SDK version the plugin was built against.</param>
    /// <param name="host">The host release number expected.</param>
    [Theory]
    [InlineData("3.0.0", "0.4.0")]
    [InlineData("3.1.0", "0.4.0")]
    [InlineData("3.2.0", "0.5.0")]
    [InlineData("3.7.0", "0.5.0")]
    [InlineData("3.8.1", "0.7.0")]
    [InlineData("3.10.0", "0.7.0")]
    public void FirstHostShipping_KnownSdk_ReturnsTheFirstHostThatCarriesIt(string sdk, string host)
    {
        PluginVersion.TryParse(sdk, out var sdkVersion).Should().BeTrue();
        PluginVersion.TryParse(host, out var expected).Should().BeTrue();

        PluginSdkHostReleases.FirstHostShipping(sdkVersion).Should().Be(expected);
    }

    /// <summary>An SDK newer than every host release ships has no host release yet.</summary>
    [Fact]
    public void FirstHostShipping_SdkNoHostShipsYet_ReturnsNull()
    {
        PluginVersion.TryParse("3.10.1", out var sdkVersion).Should().BeTrue();

        PluginSdkHostReleases.FirstHostShipping(sdkVersion).Should().BeNull();
    }
}
