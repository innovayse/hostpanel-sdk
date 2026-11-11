namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarOperationsPlugin.AddDnsRecordAsync"/>. It is the add context only: an update has <see cref="RegistrarDnsRecordUpdateContext"/> and a removal <see cref="RegistrarDnsRecordRemovalContext"/>.</summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain whose zone changes.</param>
/// <param name="Record">The record to add. A record that is not added yet has no registrar side id, so <see cref="RegistrarDnsRecord.Id"/> is <see langword="null"/>.</param>
public sealed record RegistrarDnsRecordContext(RegistrarConnection Connection, RegistrarDomain Domain, RegistrarDnsRecord Record);
