namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarOperationsPlugin.SetContactAsync"/>.</summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain.</param>
/// <param name="Contact">The registrant contact. It replaces the current one.</param>
public sealed record RegistrarContactContext(RegistrarConnection Connection, RegistrarDomain Domain, RegistrarContact Contact);
