using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>One account on a hosting panel, as far as the host needs to know it to reconcile its services against the server.</summary>
/// <param name="ProvisioningRef">The panel's reference for the account, in the same form <see cref="IProvisioningPanelPlugin.CreateAccountAsync"/> returns, so the host can match it to a service.</param>
/// <param name="Domain">The account's primary domain, or <see langword="null"/> when the panel does not say.</param>
/// <param name="Status">Whether the account is active or suspended.</param>
/// <param name="PackageName">The package the account is on, or <see langword="null"/> when the panel does not say.</param>
/// <param name="ControlPanelUrl">The address of the account's own control panel login page, or <see langword="null"/> when the panel does not say. A plain address, not a sign-in link: it carries no session or token.</param>
public sealed record PanelAccount(string ProvisioningRef, string? Domain, PanelAccountStatus Status, string? PackageName, string? ControlPanelUrl);
