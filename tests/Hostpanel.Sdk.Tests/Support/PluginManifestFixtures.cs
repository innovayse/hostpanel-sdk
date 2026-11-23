using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;

namespace Hostpanel.Sdk.Tests.Support;

/// <summary>The manifest the tests of single manifest members start from.</summary>
internal static class PluginManifestFixtures
{
    /// <summary>
    /// Builds a manifest with the required members and nothing else, but for the members a test is about. A member left
    /// <see langword="null"/> is assigned as <see langword="null"/>, which is what the tests of the defaults check.
    /// </summary>
    /// <param name="tags">The tags.</param>
    /// <param name="serverModules">The server modules.</param>
    /// <param name="serverAccountIdLabel">The label of the account id field of a server.</param>
    /// <param name="agentProfile">The deprecated SSH key agent profile name.</param>
    /// <param name="sshAgentProfile">The id of the SSH key agent profile the plugin ships.</param>
    /// <returns>The manifest.</returns>
    internal static PluginManifest Minimal(string[]? tags = null, string[]? serverModules = null, string? serverAccountIdLabel = null, string? agentProfile = null, string? sshAgentProfile = null)
    {
#pragma warning disable CS0618 // The deprecated member is still part of the manifest, so the fixture still sets it.
        return new PluginManifest
        {
            Id = "demo",
            Name = "Demo",
            Version = "1.0.0",
            Author = "A",
            Description = "D",
            Type = PluginType.Provisioning,
            Category = "Hosting",
            EntryPoint = "Demo.Plugin",
            SdkVersion = "3.7.0",
            Tags = tags!,
            ServerModules = serverModules!,
            ServerAccountIdLabel = serverAccountIdLabel,
            AgentProfile = agentProfile,
            SshAgentProfile = sshAgentProfile,
        };
#pragma warning restore CS0618
    }
}
