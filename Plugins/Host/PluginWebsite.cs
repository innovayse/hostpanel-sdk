namespace Hostpanel.Sdk.Plugins.Host;

/// <summary>A website the host holds on a hosting service for a plugin, as <see cref="IPluginHostHostingApi.EnsureWebsiteAsync"/> answers.</summary>
/// <param name="Id">The host's id of the website, for <see cref="IPluginHostHostingApi.RemoveWebsiteAsync"/>.</param>
/// <param name="Hostname">The host name the website answers to.</param>
/// <param name="DocumentRoot">The document root, relative to the hosting account's home directory.</param>
/// <param name="DocumentRootPath">The absolute path of the document root on the hosting server.</param>
/// <param name="PhpVersion">The PHP version the website runs with, or null when it has none.</param>
public sealed record PluginWebsite(
    Guid Id,
    string Hostname,
    string DocumentRoot,
    string DocumentRootPath,
    string? PhpVersion)
{
    /// <summary>
    /// Gets the absolute path of the PHP command line binary that matches <see cref="PhpVersion"/> on the hosting server,
    /// or null when the website has no PHP version or the host does not know the path. A plugin passes it to its deployer
    /// so a command runs with the same PHP as the website. It is an init property and not a constructor parameter, so
    /// every existing constructor call compiles and reads as before. Since 3.16.0.
    /// </summary>
    public string? PhpCliPath { get; init; }
}
