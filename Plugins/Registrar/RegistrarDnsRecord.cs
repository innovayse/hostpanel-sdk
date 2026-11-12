using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>One record in the DNS zone a registrar hosts for a domain.</summary>
/// <param name="Id">The registrar's own id of the record when the caller has one, for example from <see cref="IRegistrarOperationsPlugin.GetDnsRecordsAsync"/>, otherwise <see langword="null"/>. The host keeps no such id, so a plugin must be able to find a record by <paramref name="Type"/>, <paramref name="Host"/> and <paramref name="Value"/> alone and uses the id only when it is given.</param>
/// <param name="Type">The record type.</param>
/// <param name="Host">The record's host name relative to the zone, as the registrar shows it (<c>@</c> for the zone itself).</param>
/// <param name="Value">The record's value: an address, a name or a text.</param>
/// <param name="Ttl">The time to live in seconds; zero or more.</param>
/// <param name="Priority">The priority of an MX or SRV record, or <see langword="null"/> for any other type.</param>
public sealed record RegistrarDnsRecord(string? Id, RegistrarDnsRecordType Type, string Host, string Value, int Ttl, int? Priority);
