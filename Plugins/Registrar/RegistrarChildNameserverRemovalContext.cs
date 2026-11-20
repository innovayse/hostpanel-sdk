namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarChildNameserversPlugin.DeleteChildNameserverAsync"/>.</summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain the nameserver host belongs to.</param>
/// <param name="HostName">The fully qualified name of the nameserver host to delete.</param>
public sealed record RegistrarChildNameserverRemovalContext(RegistrarConnection Connection, RegistrarDomain Domain, string HostName);
