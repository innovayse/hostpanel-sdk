namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarOperationsPlugin.TestConnectionAsync"/>.</summary>
/// <param name="Connection">The registrar account to test.</param>
public sealed record RegistrarTestConnectionContext(RegistrarConnection Connection);
