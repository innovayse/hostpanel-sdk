namespace Innovayse.SDK.Plugins;

/// <summary>What a hosting panel (v7) reports about itself.</summary>
/// <param name="PackagesCount">The number of hosting packages configured on the server.</param>
public sealed record HostingPanelV7ServerInfo(int PackagesCount);
