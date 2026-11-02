namespace Innovayse.SDK.Plugins.Host;

/// <summary>
/// The kinds of access to a client's hosting a plugin may ask the host for, named in the manifest's
/// <see cref="Manifest.PluginManifest.HostAccess"/>. The admin plugins page shows each one a plugin declares.
/// </summary>
public static class PluginHostAccess
{
    /// <summary>
    /// SSH access to a hosting service of the caller whose product is bound to the plugin:
    /// <see cref="IPluginHostApi.IssueSshAccessAsync"/> and <see cref="IPluginHostApi.RevokeSshAccessAsync"/>.
    /// </summary>
    public const string Ssh = "ssh";

    /// <summary>Gets every kind this SDK version knows, in the order the admin panel lists them.</summary>
    public static IReadOnlyList<string> All { get; } = [Ssh];
}
