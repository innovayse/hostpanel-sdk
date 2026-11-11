namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarOperationsPlugin.CheckAvailabilityAsync"/>.</summary>
/// <param name="Connection">The registrar account to ask.</param>
/// <param name="DomainName">The fully qualified domain name to check.</param>
public sealed record RegistrarAvailabilityContext(RegistrarConnection Connection, string DomainName);
