namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>Input for <see cref="IProvisioningPanelPlugin.SuspendAccountAsync"/>.</summary>
/// <param name="Server">The server the account lives on.</param>
/// <param name="Service">The service and its account; <see cref="PanelService.ProvisioningRef"/> is set.</param>
/// <param name="Reason">The host's reason for the suspension, for a panel that records one; free text of the host's, not a panel code.</param>
public sealed record PanelSuspendContext(PanelServerConnection Server, PanelService Service, string Reason);
