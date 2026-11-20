namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>Input for <see cref="IProvisioningPanelPlugin.CreateSsoLinkAsync"/>.</summary>
/// <param name="Server">The server the account lives on.</param>
/// <param name="Service">The service and its account; <see cref="PanelService.ProvisioningRef"/> is set.</param>
/// <param name="CallerIp">The address of the browser that will open the link, or <see langword="null"/> when unknown; only a panel that binds the session to it reads it.</param>
public sealed record PanelSsoContext(PanelServerConnection Server, PanelService Service, string? CallerIp)
{
    /// <summary>
    /// Gets the page inside the account's control panel the link should open on (for example <c>/filemanager</c>), or
    /// <see langword="null"/> for the panel's landing page. A path that starts with a slash and carries no scheme, host or
    /// credentials: a plugin builds the address from the panel's own base and ignores a target it cannot map, or a panel with no
    /// deep link, and opens the landing page instead. Optional and null by default, so existing plugins and hosts are unchanged.
    /// </summary>
    public string? TargetPath { get; init; }
}
