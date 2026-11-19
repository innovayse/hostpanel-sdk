namespace Hostpanel.Sdk.Plugins.Host;

/// <summary>
/// What the hosting account of one service can do for a plugin, as <see cref="IPluginHostHostingApi.GetHostingCapabilitiesAsync"/>
/// reports it. A plugin reads it before it asks for a website or a database, so it can tell the client what is
/// unavailable instead of provoking a refusal.
/// </summary>
/// <param name="Ssh">Whether SSH access can be issued for the service (<see cref="PluginHostAccess.Ssh"/>).</param>
/// <param name="Websites">Whether websites can be created on the service (<see cref="PluginHostAccess.Websites"/>).</param>
/// <param name="Databases">Whether databases can be created on the service (<see cref="PluginHostAccess.Databases"/>).</param>
/// <param name="PhpVersions">The PHP versions a website of the service can use, as the host names them; empty when none.</param>
public sealed record PluginHostingCapabilities(
    bool Ssh,
    bool Websites,
    bool Databases,
    IReadOnlyList<string> PhpVersions);
