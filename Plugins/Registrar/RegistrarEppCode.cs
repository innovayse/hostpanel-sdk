using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The authorization (EPP) code that lets another registrar take a domain over. Whoever holds it can move the domain, so
/// it is a secret: the generated <c>ToString</c> prints it masked, and neither the plugin nor the host logs it.
/// </summary>
/// <param name="Code">The authorization code. Not empty. Secret.</param>
public sealed partial record RegistrarEppCode([property: SecretMember] string Code);
