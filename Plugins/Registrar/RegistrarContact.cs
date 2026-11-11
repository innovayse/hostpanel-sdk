namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The registrant contact of a domain: the details a registrar publishes in the domain's WHOIS record and uses to reach the
/// owner. The host holds one contact per domain and sends it as it is stored; the plugin converts it to what its registrar
/// wants (a phone format, a country code).
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
    string Country);
