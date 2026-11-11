namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarOperationsPlugin.GetTldPricingAsync"/>.</summary>
/// <param name="Connection">The registrar account whose price list is wanted.</param>
public sealed record RegistrarPricingContext(RegistrarConnection Connection);
