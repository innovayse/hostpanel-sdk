namespace Hostpanel.Sdk.Plugins.Host;

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

    /// <summary>
    /// Websites on a hosting service of the caller whose product is bound to the plugin:
    /// <see cref="IPluginHostHostingApi.EnsureWebsiteAsync"/> and <see cref="IPluginHostHostingApi.RemoveWebsiteAsync"/>.
    /// </summary>
    public const string Websites = "websites";

    /// <summary>
    /// Databases on a hosting service of the caller whose product is bound to the plugin:
    /// <see cref="IPluginHostHostingApi.EnsureDatabaseAsync"/> and <see cref="IPluginHostHostingApi.RemoveDatabaseAsync"/>.
    /// </summary>
    public const string Databases = "databases";

    /// <summary>
    /// The server connection of one of the caller's own services whose product is bound to the plugin:
    /// <see cref="IPluginHostServiceConnectionApi.ResolveServiceConnectionAsync"/>. Since 3.11.0.
    /// </summary>
    public const string ServiceConnection = "serviceConnection";

    /// <summary>Gets every kind this SDK version knows, in the order the admin panel lists them.</summary>
    public static IReadOnlyList<string> All { get; } = [Ssh, Websites, Databases, ServiceConnection];
}
