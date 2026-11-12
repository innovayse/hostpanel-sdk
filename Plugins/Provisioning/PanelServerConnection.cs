using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// The server a hosting panel or virtual server operation runs against: where it is and the credentials the host stores for it. The host
/// fills in what it has and a plugin reads the members its panel uses; a panel that authenticates with a user and a
/// token reads <see cref="Username"/> and <see cref="ApiToken"/>, one that uses a single key reads <see cref="AccessHash"/>.
/// <see cref="AccessHash"/> and <see cref="ApiToken"/> are secrets: the generated <c>ToString</c> prints them masked,
/// and a plugin must never log them, put them in a result or send them anywhere but its own panel.
/// </summary>
/// <param name="ServerId">The host's id of the server, for the plugin's log lines.</param>
/// <param name="Name">The server's display name, for the plugin's log lines.</param>
/// <param name="Hostname">The host name the panel answers on.</param>
/// <param name="IpAddress">The server's primary address, or <see langword="null"/> when only the host name is known.</param>
/// <param name="Port">The port of the panel's API when the host has one saved. <see langword="null"/> means the plugin's own default port; the host never guesses one for the plugin.</param>
/// <param name="UseSsl"><see langword="true"/> when the panel's API is reached over HTTPS, <see langword="false"/> for plain HTTP; the server's saved setting, which the plugin honours when it builds the panel address.</param>
/// <param name="Username">The panel user the credentials belong to, or <see langword="null"/> when the panel has no such notion or none is saved.</param>
/// <param name="AccessHash">The panel's access key or hash, or <see langword="null"/> when none is saved. Secret.</param>
/// <param name="ApiToken">The panel's API token, or <see langword="null"/> when none is saved. Secret.</param>
/// <param name="AccountId">
/// The organisation, reseller or tenant id that scopes every API call of a panel that needs one next to the credentials,
/// or <see langword="null"/> when none is saved. The operator enters it in a field of the server form that the host shows
/// only for a plugin whose manifest sets <see cref="Manifest.PluginManifest.ServerAccountIdLabel"/>, with that label. It is
/// not a secret and prints in <c>ToString</c>. A plugin reads it from here and never asks the operator to join it to
/// <see cref="Username"/>. Optional and null by default, so existing callers and plugins are unchanged.
/// </param>
public sealed record PanelServerConnection(
    int ServerId,
    string Name,
    string Hostname,
    string? IpAddress,
    int? Port,
    bool UseSsl,
    string? Username,
    string? AccessHash,
    string? ApiToken,
    string? AccountId = null)
{
    /// <summary>
    /// Prints the record without its secret members, so the generated <c>ToString</c> never reveals them.
    /// </summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("ServerId = ").Append(ServerId);
        builder.Append(", Name = ").Append(Name);
        builder.Append(", Hostname = ").Append(Hostname);
        builder.Append(", IpAddress = ").Append(IpAddress);
        builder.Append(", Port = ").Append(Port);
        builder.Append(", UseSsl = ").Append(UseSsl);
        builder.Append(", Username = ").Append(Username ?? "null");
        builder.Append(", AccessHash = ").Append(AccessHash is null ? "null" : SecretMask.Text);
        builder.Append(", ApiToken = ").Append(ApiToken is null ? "null" : SecretMask.Text);
        builder.Append(", AccountId = ").Append(AccountId ?? "null");
        return true;
    }
}
