using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The registrant contact of a domain: the details a registrar publishes in the domain's WHOIS record and uses to reach the
/// owner. The host holds one contact per domain and sends it as it is stored; the plugin converts it to what its registrar
/// wants (a phone format, a country code). It is personal data: the generated <c>ToString</c> prints every member except the
/// country masked, so a context or a result that holds a contact never leaks it into a log line or an exception message,
/// and a plugin must not undo that.
/// </summary>
/// <param name="FirstName">The registrant's first name.</param>
/// <param name="LastName">The registrant's last name.</param>
/// <param name="Organization">The organization, or <see langword="null"/> for a private person.</param>
/// <param name="Email">The e-mail address.</param>
/// <param name="Phone">The phone number, as the host stores it.</param>
/// <param name="Address1">The first address line.</param>
/// <param name="Address2">The second address line, or <see langword="null"/>.</param>
/// <param name="City">The city.</param>
/// <param name="State">The state or region.</param>
/// <param name="PostalCode">The postal code.</param>
/// <param name="Country">The country, as the host stores it.</param>
public sealed record RegistrarContact(
    string FirstName,
    string LastName,
    string? Organization,
    string Email,
    string Phone,
    string Address1,
    string? Address2,
    string City,
    string State,
    string PostalCode,
    string Country)
{
    /// <summary>Prints the record without its personal data, so the generated <c>ToString</c> never reveals it.</summary>
    /// <param name="builder">The builder the generated <c>ToString</c> is assembling.</param>
    /// <returns>Always <see langword="true"/>: members are written.</returns>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("FirstName = ").Append(SecretMask.Text);
        builder.Append(", LastName = ").Append(SecretMask.Text);
        builder.Append(", Organization = ").Append(Organization is null ? "null" : SecretMask.Text);
        builder.Append(", Email = ").Append(SecretMask.Text);
        builder.Append(", Phone = ").Append(SecretMask.Text);
        builder.Append(", Address1 = ").Append(SecretMask.Text);
        builder.Append(", Address2 = ").Append(Address2 is null ? "null" : SecretMask.Text);
        builder.Append(", City = ").Append(SecretMask.Text);
        builder.Append(", State = ").Append(SecretMask.Text);
        builder.Append(", PostalCode = ").Append(SecretMask.Text);
        builder.Append(", Country = ").Append(Country);
        return true;
    }
}
