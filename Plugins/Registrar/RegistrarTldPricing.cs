namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>What the registrar charges the host for one top level domain, per period.</summary>
/// <param name="Tld">The top level domain without a leading dot, in lower case.</param>
/// <param name="Currency">The ISO 4217 code of the currency the prices are in.</param>
/// <param name="Register">The price of a registration, by number of years. A period the registrar does not sell is missing.</param>
/// <param name="Transfer">The price of a transfer in, by number of years. A period the registrar does not sell is missing.</param>
/// <param name="Renew">The price of a renewal, by number of years. A period the registrar does not sell is missing.</param>
public sealed record RegistrarTldPricing(
    string Tld,
    string Currency,
    IReadOnlyDictionary<int, decimal> Register,
    IReadOnlyDictionary<int, decimal> Transfer,
    IReadOnlyDictionary<int, decimal> Renew);
