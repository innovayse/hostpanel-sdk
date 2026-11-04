namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>Input for <see cref="IProvisioningPanelPlugin.CreateSsoLinkAsync"/>.</summary>
/// <param name="Server">The server the account lives on.</param>
/// <param name="Service">The service and its account; <see cref="PanelService.ProvisioningRef"/> is set.</param>
/// <param name="CallerIp">The address of the browser that will open the link, or <see langword="null"/> when unknown; only a panel that binds the session to it reads it.</param>
public sealed record PanelSsoContext(PanelServerConnection Server, PanelService Service, string? CallerIp);
