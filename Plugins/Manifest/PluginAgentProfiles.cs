namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>
/// The SSH key agent profiles a panel plugin may name in <see cref="PluginManifest.AgentProfile"/>. The host keeps one
/// SSH key agent as generic infrastructure for the servers of panel plugins; a profile tells it how to talk to the
/// panel's hosts. The names are defined here, by the SDK, so they stay neutral: a name describes a kind of panel, never a
/// vendor, and a plugin cannot invent one. A name is matched exactly, with its letter case.
/// </summary>
public static class PluginAgentProfiles
{
    /// <summary>The profile for the hosting control panel family of version 7.</summary>
    public const string HostingPanelV7 = "hostingPanelV7";

    /// <summary>Gets every profile this SDK version knows, in the order the admin panel lists them.</summary>
    public static IReadOnlyList<string> All { get; } = [HostingPanelV7];

    /// <summary>
    /// Tells whether a name is a profile this SDK version defines. The name is compared exactly: no trimming and no
    /// change of letter case.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns><see langword="true"/> when the name is one of <see cref="All"/>.</returns>
    public static bool IsKnown(string? name)
    {
        return name is not null && All.Contains(name, StringComparer.Ordinal);
    }
}
