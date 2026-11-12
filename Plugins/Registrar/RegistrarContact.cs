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
public sealed partial record RegistrarContact(
    [property: SecretMember] string FirstName,
    [property: SecretMember] string LastName,
    [property: SecretMember] string? Organization,
    [property: SecretMember] string Email,
    [property: SecretMember] string Phone,
    [property: SecretMember] string Address1,
    [property: SecretMember] string? Address2,
    [property: SecretMember] string City,
    [property: SecretMember] string State,
    [property: SecretMember] string PostalCode,
    string Country);
