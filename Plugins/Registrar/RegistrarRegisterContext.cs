using System.Collections.Frozen;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Input for <see cref="IRegistrarOperationsPlugin.RegisterAsync"/>. The host sends the same context on every retry of one
/// registration, so a domain the registrar already holds for the account under <see cref="DomainName"/> is the earlier
/// attempt, not a second purchase. The members after <see cref="Contact"/> are optional and have defaults, so a context
/// built the old way still compiles and a plugin that ignores them behaves as before.
/// </summary>
/// <param name="Connection">The registrar account to register on.</param>
/// <param name="DomainName">The fully qualified domain name to register.</param>
/// <param name="Years">The registration period in years; at least 1.</param>
/// <param name="WhoisPrivacy"><see langword="true"/> to hide the registrant's details in the public WHOIS record.</param>
/// <param name="AutoRenew"><see langword="true"/> to let the registrar renew the domain by itself.</param>
/// <param name="Nameservers">The nameservers to delegate to, in order, or an empty list for the registrar's own.</param>
/// <param name="Contact">The registrant contact.</param>
public sealed partial record RegistrarRegisterContext(
    RegistrarConnection Connection,
    string DomainName,
    int Years,
    bool WhoisPrivacy,
    bool AutoRenew,
    IReadOnlyList<string> Nameservers,
    RegistrarContact Contact)
{
    /// <summary>Gets the administrative, technical and billing contacts. A role without one uses <see cref="Contact"/>: read it with <see cref="RegistrarRoleContacts.AdminOr"/> and its siblings. Never <see langword="null"/>.</summary>
    public RegistrarRoleContacts RoleContacts { get; init; } = RegistrarRoleContacts.None;

    /// <summary>Gets the attributes the top level domain's registry requires beyond the contact (a company number, a nexus category, a language), by the registry's own key. Empty when none. The values can be personal data, so they print masked. Never <see langword="null"/>.</summary>
    [SecretMember]
    public IReadOnlyDictionary<string, string> ExtendedAttributes { get; init; } = FrozenDictionary<string, string>.Empty;

    /// <summary>Gets the end user's acceptance of the registrar's terms, for a registrar that records consent, or <see langword="null"/> when the host has none.</summary>
    public RegistrarAgreement? Agreement { get; init; }
}
