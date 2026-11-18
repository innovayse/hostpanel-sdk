namespace Hostpanel.Sdk.Plugins.Host;

/// <summary>What a plugin asks <see cref="IPluginHostHostingApi.EnsureWebsiteAsync"/> to set up on a hosting service.</summary>
/// <param name="Hostname">The host name the website answers to.</param>
/// <param name="DocumentRoot">The document root, relative to the hosting account's home directory.</param>
/// <param name="PhpVersion">The PHP version to run the website with, or null for the host's default.</param>
/// <param name="DocumentRootMayBeLink">
/// True when the plugin will later switch the document root through a symbolic link, so the host applies the pool
/// settings that allow a document root to be a link; false for an ordinary directory.
/// </param>
public sealed record PluginWebsiteSpec(
    string Hostname,
    string DocumentRoot,
    string? PhpVersion,
    bool DocumentRootMayBeLink);
