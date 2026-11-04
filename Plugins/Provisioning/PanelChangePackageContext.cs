namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>Input for <see cref="IProvisioningPanelPlugin.ChangePackageAsync"/>.</summary>
/// <param name="Server">The server the account lives on.</param>
/// <param name="Service">The service and its account; <see cref="PanelService.ProvisioningRef"/> is set.</param>
/// <param name="Package">The package the account moves to.</param>
public sealed record PanelChangePackageContext(PanelServerConnection Server, PanelService Service, PanelPackage Package);
