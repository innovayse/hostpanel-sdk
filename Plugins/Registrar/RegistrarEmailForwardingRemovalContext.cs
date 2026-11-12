using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarEmailForwardingPlugin.DeleteEmailForwardingRuleAsync"/>. The source can be a personal e-mail address, so the generated <c>ToString</c> prints it masked.</summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain whose forwarding changes.</param>
/// <param name="Source">The source of the rule to remove, as the host stores it. Personal data.</param>
public sealed partial record RegistrarEmailForwardingRemovalContext(RegistrarConnection Connection, RegistrarDomain Domain, [property: SecretMember] string Source);
