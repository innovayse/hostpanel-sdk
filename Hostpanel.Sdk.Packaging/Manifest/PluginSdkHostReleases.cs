using Hostpanel.Sdk.Packaging.Naming;

namespace Hostpanel.Sdk.Packaging.Manifest;

/// <summary>
/// Which host release first shipped which SDK, the one table behind the rule that a plugin's <c>minHostVersion</c> is
/// not below the first host that can load the SDK it was built against. A row is added when a host release ships a new SDK
/// version, and holds that SDK version, not the ones between it and the row before: an SDK in between is carried by the
/// first host that ships a newer row. An SDK version no host release ships yet has no row, so no host can load a plugin
/// built on it. A new minor or major SDK version fails the table's test until its row is added.
/// </summary>
public static class PluginSdkHostReleases
{
    /// <summary>Each host release that moved to a new SDK version: the SDK it shipped, and the host release number, oldest first.</summary>
    private static readonly IReadOnlyList<(PluginVersion Sdk, PluginVersion Host)> Releases =
    [
        (Parse("3.1.0"), Parse("0.4.0")),
        (Parse("3.7.0"), Parse("0.5.0")),
        (Parse("3.10.0"), Parse("0.7.0")),
        (Parse("3.13.0"), Parse("0.9.0")),
        (Parse("3.16.0"), Parse("0.10.0")),
    ];

    /// <summary>Finds the first host release whose SDK is at least as new as the one a plugin was built against.</summary>
    /// <param name="sdkVersion">The SDK version the plugin was built against.</param>
    /// <returns>
    /// The host release number; <see langword="null"/> when no host release ships an SDK that new, because such a plugin
    /// has no host to run on yet and no <c>minHostVersion</c> can be right for it.
    /// </returns>
    public static PluginVersion? FirstHostShipping(PluginVersion sdkVersion)
    {
        ArgumentNullException.ThrowIfNull(sdkVersion);

        foreach (var (sdk, host) in Releases)
        {
            if (sdk.CompareTo(sdkVersion) >= 0)
            {
                return host;
            }
        }

        return null;
    }

    /// <summary>Reads a version written in this file; a malformed entry is a defect in the table and fails loudly.</summary>
    /// <param name="text">The version text.</param>
    /// <returns>The version.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the text is not a version.</exception>
    private static PluginVersion Parse(string text)
    {
        if (!PluginVersion.TryParse(text, out var version))
        {
            throw new InvalidOperationException($"The SDK and host release table holds '{text}', which is not a version.");
        }

        return version;
    }
}
