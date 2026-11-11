namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarEmailForwardingPlugin.DeleteEmailForwardingRuleAsync"/>.</summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain whose forwarding changes.</param>
/// <param name="Source">The source of the rule to remove, as the host stores it.</param>
public sealed record RegistrarEmailForwardingRemovalContext(RegistrarConnection Connection, RegistrarDomain Domain, string Source);
