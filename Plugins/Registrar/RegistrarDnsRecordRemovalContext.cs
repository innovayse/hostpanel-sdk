namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Input for <see cref="IRegistrarOperationsPlugin.DeleteDnsRecordAsync"/>. The host keeps no registrar side id for a record,
/// so the removal carries the whole record, and the plugin finds it by type, host and value (or by
/// <see cref="RegistrarDnsRecord.Id"/> when the host has one).
/// </summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain whose zone changes.</param>
/// <param name="Record">The record to remove.</param>
public sealed record RegistrarDnsRecordRemovalContext(RegistrarConnection Connection, RegistrarDomain Domain, RegistrarDnsRecord Record);
