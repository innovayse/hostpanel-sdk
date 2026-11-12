namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Input for <see cref="IRegistrarOperationsPlugin.UpdateDnsRecordAsync"/>. The host keeps no registrar side id for a record,
/// so the update carries the record as the host knew it and the content it should have now, and the plugin finds the record
/// by type, host and value (or by <see cref="RegistrarDnsRecord.Id"/> when the host has one) before it changes it.
/// </summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain whose zone changes.</param>
/// <param name="Current">The record as it is now. Its type, host and value find the record; its id is optional.</param>
/// <param name="Updated">The content the record should have afterwards.</param>
public sealed record RegistrarDnsRecordUpdateContext(
    RegistrarConnection Connection,
    RegistrarDomain Domain,
    RegistrarDnsRecord Current,
    RegistrarDnsRecord Updated);
