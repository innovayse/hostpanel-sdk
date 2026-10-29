namespace Innovayse.SDK.Plugins;

/// <summary>What a hosting panel reports about itself.</summary>
/// <param name="AccountsCount">The number of hosting accounts on the server.</param>
/// <param name="Version">The panel's own version string.</param>
public sealed record HostingPanelServerInfo(int AccountsCount, string Version);
