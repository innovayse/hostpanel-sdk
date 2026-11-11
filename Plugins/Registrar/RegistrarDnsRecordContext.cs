namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarOperationsPlugin.AddDnsRecordAsync"/> and <see cref="IRegistrarOperationsPlugin.UpdateDnsRecordAsync"/>.</summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain whose zone changes.</param>
/// <param name="Record">The record to add, or the new content of the record to update; an update names the record in <see cref="RegistrarDnsRecord.Id"/>.</param>
public sealed record RegistrarDnsRecordContext(RegistrarConnection Connection, RegistrarDomain Domain, RegistrarDnsRecord Record);
