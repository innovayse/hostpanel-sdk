using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The registrar account an operation runs against: the credentials and options the admin saved for the plugin. The host
/// fills in what it has and a plugin reads the members its registrar uses; a plugin whose registrar needs a setting this
/// record does not have reads it with <see cref="Base.RegistrarProviderBase"/>. One plugin serves one registrar account,
/// and the connection arrives in each call, so the plugin keeps no state between calls. <see cref="ApiKey"/> is a secret:
/// the generated <c>ToString</c> prints it masked, and a plugin must never log it, put it in a result or send it anywhere
/// but its own registrar.
/// </summary>
/// <param name="AccountName">The registrar account the credentials belong to (a user name or an e-mail address), or <see langword="null"/> when none is saved. It can be an e-mail address, so it prints masked.</param>
/// <param name="ApiKey">The registrar's API key or password, or <see langword="null"/> when none is saved. Secret.</param>
/// <param name="ApiUrl">The address of the registrar's API when the admin saved one, or <see langword="null"/> for the plugin's own default. An absolute address.</param>
/// <param name="ClientIp">The address the registrar has allow-listed for API calls, for a registrar that asks for it with every request, or <see langword="null"/>.</param>
/// <param name="TestMode"><see langword="true"/> when the admin asked for the registrar's sandbox, which a plugin honours by calling the sandbox API and never the live one.</param>
public sealed record RegistrarConnection(
    string? AccountName,
    string? ApiKey,
    string? ApiUrl,
    string? ClientIp,
    bool TestMode)
{
    /// <summary>Prints the record without its secret member, so the generated <c>ToString</c> never reveals it.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("AccountName = ").Append(AccountName is null ? "null" : SecretMask.Text);
        builder.Append(", ApiKey = ").Append(ApiKey is null ? "null" : SecretMask.Text);
        builder.Append(", ApiUrl = ").Append(ApiUrl);
        builder.Append(", ClientIp = ").Append(ClientIp);
        builder.Append(", TestMode = ").Append(TestMode);
        return true;
    }
}
