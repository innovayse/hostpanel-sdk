namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Input for <see cref="IRegistrarOperationsPlugin.RegisterAsync"/>. The host sends the same context on every retry of one
/// registration, so a domain the registrar already holds for the account under <see cref="DomainName"/> is the earlier
/// attempt, not a second purchase.
/// </summary>
/// <param name="Connection">The registrar account to register on.</param>
/// <param name="DomainName">The fully qualified domain name to register.</param>
/// <param name="Years">The registration period in years; at least 1.</param>
/// <param name="WhoisPrivacy"><see langword="true"/> to hide the registrant's details in the public WHOIS record.</param>
/// <param name="AutoRenew"><see langword="true"/> to let the registrar renew the domain by itself.</param>
/// <param name="Nameservers">The nameservers to delegate to, in order, or an empty list for the registrar's own.</param>
/// <param name="Contact">The registrant contact.</param>
public sealed record RegistrarRegisterContext(
    RegistrarConnection Connection,
    string DomainName,
    int Years,
    bool WhoisPrivacy,
    bool AutoRenew,
    IReadOnlyList<string> Nameservers,
    RegistrarContact Contact);
