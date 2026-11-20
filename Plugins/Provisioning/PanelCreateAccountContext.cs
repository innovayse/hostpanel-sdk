using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Input for <see cref="IProvisioningPanelPlugin.CreateAccountAsync"/>. The host chooses <see cref="Username"/> and stores
/// it before it calls the plugin, so a retry of the same create sends the same user name: that is what makes the call
/// idempotent. <see cref="Password"/> is a secret and is printed masked.
/// </summary>
/// <param name="Server">The server to create the account on.</param>
/// <param name="Service">The service the account is for; <see cref="PanelService.ProvisioningRef"/> is <see langword="null"/>.</param>
/// <param name="Client">The client the account is for: the reference, e-mail address and name the host has. Required; the e-mail address and the name inside it are optional because the host may not have them.</param>
/// <param name="Package">The package to create the account on.</param>
/// <param name="DomainName">The primary domain of the account.</param>
/// <param name="Username">The panel user name for the account. Stable across retries of one create.</param>
/// <param name="Password">The initial password of the account. Secret.</param>
public sealed partial record PanelCreateAccountContext(
    PanelServerConnection Server,
    PanelService Service,
    PanelClientRef Client,
    PanelPackage Package,
    string DomainName,
    string Username,
    [property: SecretMember] string Password)
{
    /// <summary>
    /// Gets the kind of account to create, or <see langword="null"/> for the panel's default, an ordinary user account. A
    /// plugin whose panel has no resellers refuses a reseller create with <see cref="PanelRefusalReason.Other"/> and never
    /// creates a user account in its place. Optional and null by default, so existing plugins and hosts are unchanged.
    /// </summary>
    public PanelAccountLevel? Level { get; init; }
}
