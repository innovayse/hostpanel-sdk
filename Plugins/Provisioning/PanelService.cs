namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>The host's service an operation acts for, and the account the panel holds for it.</summary>
/// <param name="ServiceId">The host's id of the service, for the plugin's log lines. Never a panel identifier.</param>
/// <param name="ProvisioningRef">
/// The panel's reference for the account, exactly as the plugin returned it from
/// <see cref="IProvisioningPanelPlugin.CreateAccountAsync"/>. <see langword="null"/> only for a create, which has no
/// account yet; every other operation gets it set.
/// </param>
public sealed record PanelService(int ServiceId, string? ProvisioningRef);
