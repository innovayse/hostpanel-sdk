using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// One e-mail forwarding rule of a domain: mail sent to the source address is delivered to the destination. Both can be
/// personal e-mail addresses, so the generated <c>ToString</c> prints them masked.
/// </summary>
/// <param name="Source">The address or mailbox name on the domain that receives the mail, as the host stores it. Personal data.</param>
/// <param name="Destination">The address the mail is forwarded to. Personal data.</param>
public sealed record RegistrarEmailForwardingRule(string Source, string Destination)
{
    /// <summary>Prints the record without its addresses, so the generated <c>ToString</c> never reveals them.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Source = ").Append(SecretMask.Text);
        builder.Append(", Destination = ").Append(SecretMask.Text);
        return true;
    }
}
