namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// The member of a <see cref="Registrar.RegistrarConnection"/> that one plugin setting fills, written in the
/// <c>connectionRole</c> member of a <c>settings</c> entry of <c>plugin.json</c> (for example <c>apiKey</c>). Values
/// start at 1; 0 is not a role.
/// </summary>
public enum RegistrarConnectionRole
{
    /// <summary>The setting holds the registrar account name: <see cref="Registrar.RegistrarConnection.AccountName"/>.</summary>
    AccountName = 1,

    /// <summary>The setting holds the registrar's API key or password: <see cref="Registrar.RegistrarConnection.ApiKey"/>. It must be a secret setting.</summary>
    ApiKey = 2,

    /// <summary>The setting holds the address of the registrar's API: <see cref="Registrar.RegistrarConnection.ApiUrl"/>. It must be a url setting.</summary>
    ApiUrl = 3,

    /// <summary>The setting holds the address the registrar has allow-listed for API calls: <see cref="Registrar.RegistrarConnection.ClientIp"/>.</summary>
    ClientIp = 4,

    /// <summary>The setting switches the registrar's sandbox on: <see cref="Registrar.RegistrarConnection.TestMode"/>. It must be a boolean setting.</summary>
    TestMode = 5,
}
