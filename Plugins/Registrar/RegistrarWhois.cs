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
public sealed partial record RegistrarWhois(string Registrar, [property: SecretMember] string Registrant, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
