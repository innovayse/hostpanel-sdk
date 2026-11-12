namespace Hostpanel.Sdk.Plugins.HostingPanel;

/// <summary>What a hosting panel (v7) reports about itself.</summary>
/// <param name="PackagesCount">The number of hosting packages configured on the server.</param>
[Obsolete("Superseded by Hostpanel.Sdk.Plugins.Provisioning.IProvisioningPanelPlugin: a panel plugin implements it and reaches its panel through IPanelTransport. This type will be removed in 4.0.0.")]
public sealed record HostingPanelV7ServerInfo(int PackagesCount);
