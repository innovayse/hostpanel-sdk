namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>What the registrar account can still spend on orders.</summary>
/// <param name="Currency">The ISO 4217 code of the currency the amount is in.</param>
/// <param name="Amount">The available balance; zero or more.</param>
public sealed record RegistrarBalance(string Currency, decimal Amount);
