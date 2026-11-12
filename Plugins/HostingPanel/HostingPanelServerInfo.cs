namespace Hostpanel.Sdk.Plugins.HostingPanel;

/// <summary>What a hosting panel reports about itself.</summary>
/// <param name="AccountsCount">The number of hosting accounts on the server.</param>
/// <param name="Version">The panel's own version string.</param>
[Obsolete("Superseded by Hostpanel.Sdk.Plugins.Provisioning.IProvisioningPanelPlugin: a panel plugin implements it and reaches its panel through IPanelTransport. This type will be removed in 4.0.0.")]
public sealed record HostingPanelServerInfo(int AccountsCount, string Version);
