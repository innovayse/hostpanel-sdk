namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarChildNameserversPlugin.CreateChildNameserverAsync"/> and <see cref="IRegistrarChildNameserversPlugin.UpdateChildNameserverAsync"/>.</summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain the nameserver host belongs to.</param>
/// <param name="HostName">The fully qualified name of the nameserver host, a name under <paramref name="Domain"/> (for example <c>ns1.example.test</c>).</param>
/// <param name="IpAddresses">The complete list of addresses (IPv4 and IPv6) the host resolves to. An update replaces the current list.</param>
public sealed record RegistrarChildNameserverContext(
    RegistrarConnection Connection,
    RegistrarDomain Domain,
    string HostName,
    IReadOnlyList<string> IpAddresses);
