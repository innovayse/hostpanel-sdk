using System.Reflection;
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
    [InlineData("3.11.0", "0.9.0")]
    [InlineData("3.12.0", "0.9.0")]
    [InlineData("3.13.0", "0.9.0")]
    [InlineData("3.14.0", "0.10.0")]
    [InlineData("3.15.0", "0.10.0")]
    [InlineData("3.16.0", "0.10.0")]
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
        PluginVersion.TryParse("3.99.0", out var sdkVersion).Should().BeTrue();

        PluginSdkHostReleases.FirstHostShipping(sdkVersion).Should().BeNull();
    }

    /// <summary>A newer SDK never maps to an older host release, which is what a table kept oldest first guarantees.</summary>
    [Fact]
    public void FirstHostShipping_AscendingSdkVersions_NeverReturnsAnOlderHost()
    {
        PluginVersion? previous = null;
        for (var minor = 0; minor <= 16; minor++)
        {
            PluginVersion.TryParse($"3.{minor}.0", out var sdkVersion).Should().BeTrue();

            var host = PluginSdkHostReleases.FirstHostShipping(sdkVersion);

            host.Should().NotBeNull();
            if (previous is not null)
            {
                host!.CompareTo(previous).Should().BeGreaterThanOrEqualTo(0);
            }

            previous = host;
        }
    }

    /// <summary>
    /// The table reaches the SDK this package is built as: a new minor or major SDK version is not published without the row
    /// that names the host release shipping it, so a plugin built on it is never left without a lower bound to check.
    /// </summary>
    [Fact]
    public void FirstHostShipping_SdkOfThisPackage_HasARow()
    {
        var packageVersion = typeof(PluginSdkHostReleases).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        PluginVersion.TryParse(packageVersion.Split('+')[0], out var current).Should().BeTrue();
        PluginVersion.TryParse($"{current.Major}.{current.Minor}.0", out var firstOfSeries).Should().BeTrue();

        PluginSdkHostReleases.FirstHostShipping(firstOfSeries).Should().NotBeNull(
            $"SDK {packageVersion} is new, so add the row for the host release that ships SDK {firstOfSeries.Major}.{firstOfSeries.Minor}.0 to PluginSdkHostReleases");
    }
}
