using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The registrar account an operation runs against: the credentials and options the admin saved for the plugin. The host
/// fills in what it has and a plugin reads the members its registrar uses; a plugin whose registrar needs a setting this
/// record does not have reads it with <see cref="Base.RegistrarProviderBase"/>. One plugin serves one registrar account,
/// and the connection arrives in each call, so the plugin keeps no state between calls. <see cref="ApiKey"/> is a secret:
/// the generated <c>ToString</c> prints it masked, and a plugin must never log it, put it in a result or send it anywhere
/// but its own registrar. The host builds the connection from the plugin's saved settings: a setting of the manifest says
/// which member it fills in <see cref="Manifest.PluginSetting.ConnectionRole"/> (a <see cref="Enums.RegistrarConnectionRole"/>),
/// whatever the plugin names the setting.
/// </summary>
/// <param name="AccountName">The registrar account the credentials belong to (a user name or an e-mail address), or <see langword="null"/> when none is saved. It can be an e-mail address, so it prints masked. Filled from the setting with the role <see cref="Enums.RegistrarConnectionRole.AccountName"/>.</param>
/// <param name="ApiKey">The registrar's API key or password, or <see langword="null"/> when none is saved. Secret. Filled from the secret setting with the role <see cref="Enums.RegistrarConnectionRole.ApiKey"/>.</param>
/// <param name="ApiUrl">The address of the registrar's API when the admin saved one, or <see langword="null"/> for the plugin's own default. An absolute address. Filled from the url setting with the role <see cref="Enums.RegistrarConnectionRole.ApiUrl"/>.</param>
/// <param name="ClientIp">The address the registrar has allow-listed for API calls, for a registrar that asks for it with every request, or <see langword="null"/>. Filled from the setting with the role <see cref="Enums.RegistrarConnectionRole.ClientIp"/>.</param>
/// <param name="TestMode"><see langword="true"/> when the admin asked for the registrar's sandbox, which a plugin honours by calling the sandbox API and never the live one. Filled from the boolean setting with the role <see cref="Enums.RegistrarConnectionRole.TestMode"/>.</param>
public sealed partial record RegistrarConnection(
    [property: SecretMember] string? AccountName,
    [property: SecretMember] string? ApiKey,
    string? ApiUrl,
    string? ClientIp,
    bool TestMode);
