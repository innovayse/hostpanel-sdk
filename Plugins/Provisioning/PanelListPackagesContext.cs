namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>Input for <see cref="IProvisioningPanelPackagesPlugin.ListPackagesAsync"/>.</summary>
/// <param name="Server">The server whose packages are listed.</param>
public sealed record PanelListPackagesContext(PanelServerConnection Server);
