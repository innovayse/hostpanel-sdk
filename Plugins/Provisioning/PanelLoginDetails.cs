using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// How a client signs in to the control panel of one hosted account, as <see cref="IProvisioningPanelLoginPlugin.GetLoginDetailsAsync"/>
/// answers it. The host shows the details to the client. The password is a secret: the generated <c>ToString</c> prints it
/// masked, and neither the plugin nor the host logs it.
/// </summary>
/// <param name="Username">The user name the account signs in with. Not empty.</param>
/// <param name="Password">The password, or <see langword="null"/> when the panel never reveals a stored password, which is the usual case. Secret.</param>
/// <param name="Domain">The primary domain of the account, or <see langword="null"/> when the panel has none to report.</param>
/// <param name="ServerIp">The address of the server the account lives on, or <see langword="null"/> when the plugin does not know it.</param>
/// <param name="LoginUrl">The absolute <c>http</c> or <c>https</c> address of the panel's sign-in page for this account. Not a single sign on link: it carries no credential.</param>
public sealed partial record PanelLoginDetails(
    string Username,
    [property: SecretMember] string? Password,
    string? Domain,
    string? ServerIp,
    string LoginUrl);
