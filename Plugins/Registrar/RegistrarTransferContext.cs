using System.Collections.Frozen;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Input for <see cref="IRegistrarOperationsPlugin.TransferAsync"/>. The host sends the same context on every retry of one
/// transfer, so a transfer the registrar already holds for the domain is the earlier attempt. <see cref="EppCode"/> is a
/// secret and is printed masked. The members after <see cref="Contact"/> are optional and have defaults, so a context built
/// the old way still compiles and a plugin that ignores them behaves as before.
/// </summary>
/// <param name="Connection">The registrar account to transfer to.</param>
/// <param name="DomainName">The fully qualified domain name to transfer in.</param>
/// <param name="EppCode">The authorization code from the current registrar. Secret.</param>
/// <param name="WhoisPrivacy"><see langword="true"/> to hide the registrant's details in the public WHOIS record.</param>
/// <param name="Contact">The registrant contact.</param>
public sealed partial record RegistrarTransferContext(
    RegistrarConnection Connection,
    string DomainName,
    [property: SecretMember] string EppCode,
    bool WhoisPrivacy,
    RegistrarContact Contact)
{
    /// <summary>Gets the years the transfer adds to the registration; at least 1. A transfer in usually renews the domain for one year, which is the default.</summary>
    public int Years { get; init; } = 1;

    /// <summary>Gets the administrative, technical and billing contacts. A role without one uses <see cref="Contact"/>: read it with <see cref="RegistrarRoleContacts.AdminOr"/> and its siblings. Never <see langword="null"/>.</summary>
    public RegistrarRoleContacts RoleContacts { get; init; } = RegistrarRoleContacts.None;

    /// <summary>Gets the attributes the top level domain's registry requires beyond the contact. The keys are the keys of the domain fields the manifest declares for the domain (<see cref="Manifest.PluginDomainFieldRules.Match"/>), unchanged, and the host has validated the values against them; dates are <c>yyyy-MM-dd</c>. Empty when none. The values can be personal data, so they print masked. Never <see langword="null"/>.</summary>
    [SecretMember]
    public IReadOnlyDictionary<string, string> ExtendedAttributes { get; init; } = FrozenDictionary<string, string>.Empty;

    /// <summary>Gets the end user's acceptance of the registrar's terms, for a registrar that records consent, or <see langword="null"/> when the host has none.</summary>
    public RegistrarAgreement? Agreement { get; init; }
}
