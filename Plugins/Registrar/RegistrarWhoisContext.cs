namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarWhoisPlugin.GetWhoisAsync"/>. The name need not be a domain the registrar holds.</summary>
/// <param name="Connection">The registrar account to ask.</param>
/// <param name="DomainName">The fully qualified domain name to look up.</param>
public sealed record RegistrarWhoisContext(RegistrarConnection Connection, string DomainName);
