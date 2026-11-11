using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Input for <see cref="IRegistrarOperationsPlugin.TransferAsync"/>. The host sends the same context on every retry of one
/// transfer, so a transfer the registrar already holds for the domain is the earlier attempt. <see cref="EppCode"/> is a
/// secret and is printed masked.
/// </summary>
/// <param name="Connection">The registrar account to transfer to.</param>
/// <param name="DomainName">The fully qualified domain name to transfer in.</param>
/// <param name="EppCode">The authorization code from the current registrar. Secret.</param>
/// <param name="WhoisPrivacy"><see langword="true"/> to hide the registrant's details in the public WHOIS record.</param>
/// <param name="Contact">The registrant contact.</param>
public sealed record RegistrarTransferContext(
    RegistrarConnection Connection,
    string DomainName,
    string EppCode,
    bool WhoisPrivacy,
    RegistrarContact Contact)
{
    /// <summary>Prints the record without its secret member, so the generated <c>ToString</c> never reveals it.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Connection = ").Append(Connection);
        builder.Append(", DomainName = ").Append(DomainName);
        builder.Append(", EppCode = ").Append(SecretMask.Text);
        builder.Append(", WhoisPrivacy = ").Append(WhoisPrivacy);
        builder.Append(", Contact = ").Append(Contact);
        return true;
    }
}
