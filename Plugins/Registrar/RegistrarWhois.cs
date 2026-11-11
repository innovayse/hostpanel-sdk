using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// What a WHOIS lookup of a domain name answers. The registrant can be a person, so it is personal data: the generated
/// <c>ToString</c> prints it masked.
/// </summary>
/// <param name="Registrar">The name of the registrar that holds the domain.</param>
/// <param name="Registrant">The registrant as the WHOIS record shows it. Personal data.</param>
/// <param name="CreatedAt">When the domain was first registered.</param>
/// <param name="ExpiresAt">When the registration ends.</param>
public sealed record RegistrarWhois(string Registrar, string Registrant, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt)
{
    /// <summary>Prints the record without the registrant, so the generated <c>ToString</c> never reveals it.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Registrar = ").Append(Registrar);
        builder.Append(", Registrant = ").Append(SecretMask.Text);
        builder.Append(", CreatedAt = ").Append(CreatedAt);
        builder.Append(", ExpiresAt = ").Append(ExpiresAt);
        return true;
    }
}
