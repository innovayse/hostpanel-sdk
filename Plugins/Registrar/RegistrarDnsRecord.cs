using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// One record in the DNS zone a registrar hosts for a domain. <see cref="Proxied"/> and <see cref="Comment"/> are optional
/// members with a default, so a record built the old way still compiles and a plugin that ignores them behaves as before.
/// </summary>
/// <param name="Id">The registrar's own id of the record when the caller has one, for example from <see cref="IRegistrarOperationsPlugin.GetDnsRecordsAsync"/>, otherwise <see langword="null"/>. The host keeps no such id, so a plugin must be able to find a record by <paramref name="Type"/>, <paramref name="Host"/> and <paramref name="Value"/> alone and uses the id only when it is given.</param>
/// <param name="Type">The record type.</param>
/// <param name="Host">The record's host name relative to the zone, as the registrar shows it (<c>@</c> for the zone itself).</param>
/// <param name="Value">The record's value: an address, a name or a text.</param>
/// <param name="Ttl">The time to live in seconds; zero or more.</param>
/// <param name="Priority">The priority of an MX or SRV record, or <see langword="null"/> for any other type.</param>
public sealed record RegistrarDnsRecord(string? Id, RegistrarDnsRecordType Type, string Host, string Value, int Ttl, int? Priority)
{
    /// <summary>
    /// Gets whether the registrar's proxy or content network answers for the record, for a registrar that has one, or
    /// <see langword="null"/> to leave the registrar's own default. A plugin whose registrar has no such feature ignores it.
    /// </summary>
    public bool? Proxied { get; init; }

    /// <summary>Gets a free text note kept with the record, for a registrar that stores one, or <see langword="null"/> for none. A plugin whose registrar has no such field ignores it.</summary>
    public string? Comment { get; init; }
}
