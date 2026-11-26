using System.Collections.Frozen;
using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Input for <see cref="IRegistrarOperationsPlugin.SetContactAsync"/>. The members after <see cref="Contact"/> are optional
/// and have defaults, so a context built the old way still compiles and a plugin that ignores them behaves as before.
/// </summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain.</param>
/// <param name="Contact">The registrant contact. It replaces the current one.</param>
public sealed partial record RegistrarContactContext(RegistrarConnection Connection, RegistrarDomain Domain, RegistrarContact Contact)
{
    /// <summary>Gets the administrative, technical and billing contacts. They replace the current ones; a role without one uses <see cref="Contact"/>: read it with <see cref="RegistrarRoleContacts.AdminOr"/> and its siblings. Never <see langword="null"/>.</summary>
    public RegistrarRoleContacts RoleContacts { get; init; } = RegistrarRoleContacts.None;

    /// <summary>Gets the attributes the top level domain's registry requires beyond the contact. The keys are the keys of the domain fields the manifest declares for the domain (<see cref="Manifest.PluginDomainFieldRules.Match"/>), unchanged, and the host has validated the values against them; dates are <c>yyyy-MM-dd</c>. Empty when none. The values can be personal data, so they print masked. Never <see langword="null"/>.</summary>
    [SecretMember]
    public IReadOnlyDictionary<string, string> ExtendedAttributes { get; init; } = FrozenDictionary<string, string>.Empty;
}
