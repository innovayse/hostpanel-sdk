namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>One delegation signer (DS) record of a domain, the DNSSEC link between the domain and its parent zone.</summary>
/// <param name="KeyTag">The key tag of the signing key; 0 to 65535.</param>
/// <param name="Algorithm">The DNSSEC algorithm number of the signing key, as the IANA registry numbers it.</param>
/// <param name="DigestType">The digest type number, as the IANA registry numbers it.</param>
/// <param name="Digest">The digest of the signing key, in hexadecimal.</param>
public sealed record RegistrarDsRecord(int KeyTag, int Algorithm, int DigestType, string Digest);
