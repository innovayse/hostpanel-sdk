using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The proof that the end user accepted the registrar's terms, for a registrar that records the consent of a registration or
/// a transfer: the address the user ordered from and the moment they accepted. The address is personal data, so the
/// generated <c>ToString</c> prints it masked.
/// </summary>
/// <param name="IpAddress">The IP address of the end user at the moment of acceptance, in text form (IPv4 or IPv6). Personal data.</param>
/// <param name="AcceptedAt">The moment the end user accepted the terms.</param>
public sealed partial record RegistrarAgreement([property: SecretMember] string IpAddress, DateTimeOffset AcceptedAt);
