namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Input for an operation that needs only the registrar account and the domain: the transfer cancel and the outgoing
/// transfer, the authorization code, the nameserver, contact and DNS record reads, and the check on a pending order.
/// </summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain.</param>
public sealed record RegistrarDomainContext(RegistrarConnection Connection, RegistrarDomain Domain);
