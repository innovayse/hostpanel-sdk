namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>What a registrar charges the host for a premium domain name, which costs more than the standard price of its top level domain.</summary>
/// <param name="Currency">The ISO 4217 code of the currency the prices are in.</param>
/// <param name="Register">The price of a registration, for the period the registrar sells it for.</param>
/// <param name="Renew">The price of a renewal, or <see langword="null"/> when the registrar does not say (the host then uses the registration price).</param>
public sealed record RegistrarPremiumPrice(string Currency, decimal Register, decimal? Renew);
