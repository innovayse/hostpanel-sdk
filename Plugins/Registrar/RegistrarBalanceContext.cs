namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarBalancePlugin.GetBalanceAsync"/>.</summary>
/// <param name="Connection">The registrar account whose balance is wanted.</param>
public sealed record RegistrarBalanceContext(RegistrarConnection Connection);
