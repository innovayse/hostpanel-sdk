using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>Input for <see cref="IProvisioningPanelPlugin.ChangePasswordAsync"/>. <see cref="NewPassword"/> is a secret and is printed masked.</summary>
/// <param name="Server">The server the account lives on.</param>
/// <param name="Service">The service and its account; <see cref="PanelService.ProvisioningRef"/> is set.</param>
/// <param name="NewPassword">The password to set. Secret.</param>
public sealed partial record PanelChangePasswordContext(PanelServerConnection Server, PanelService Service, [property: SecretMember] string NewPassword);
