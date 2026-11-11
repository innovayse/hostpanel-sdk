namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarOperationsPlugin.SetNameserversAsync"/>.</summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain.</param>
/// <param name="Nameservers">The complete list of nameservers the domain delegates to, in order. It replaces the current list.</param>
public sealed record RegistrarNameserversContext(RegistrarConnection Connection, RegistrarDomain Domain, IReadOnlyList<string> Nameservers);
