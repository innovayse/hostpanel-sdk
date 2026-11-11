namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>Input for <see cref="IRegistrarOperationsPlugin.DeleteDnsRecordAsync"/>.</summary>
/// <param name="Connection">The registrar account the domain is held on.</param>
/// <param name="Domain">The domain whose zone changes.</param>
/// <param name="RecordId">The registrar's id of the record to remove, as <see cref="IRegistrarOperationsPlugin.GetDnsRecordsAsync"/> returned it in <see cref="RegistrarDnsRecord.Id"/>.</param>
public sealed record RegistrarDnsRecordRemovalContext(RegistrarConnection Connection, RegistrarDomain Domain, string RecordId);
