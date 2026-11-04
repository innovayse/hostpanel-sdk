namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Input for an operation that needs only the server and the account: <see cref="IProvisioningPanelPlugin.UnsuspendAccountAsync"/>
/// and <see cref="IProvisioningPanelPlugin.GetUsageAsync"/>.
/// </summary>
/// <param name="Server">The server the account lives on.</param>
/// <param name="Service">The service and its account; <see cref="PanelService.ProvisioningRef"/> is set.</param>
public sealed record PanelAccountContext(PanelServerConnection Server, PanelService Service);
