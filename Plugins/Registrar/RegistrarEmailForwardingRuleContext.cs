namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarEmailForwardingPlugin.AddEmailForwardingRuleAsync"/> and <see cref="IRegistrarEmailForwardingPlugin.UpdateEmailForwardingRuleAsync"/>.</summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain whose forwarding changes.</param>
/// <param name="Rule">The rule to add, or the new content of the rule whose source it names (an update changes the destination of that source).</param>
public sealed record RegistrarEmailForwardingRuleContext(RegistrarConnection Connection, RegistrarDomain Domain, RegistrarEmailForwardingRule Rule);
