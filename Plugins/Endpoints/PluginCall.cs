using Innovayse.SDK.Plugins.Enums;
using Innovayse.SDK.Plugins.Host;
using Innovayse.SDK.Plugins.Store;

namespace Innovayse.SDK.Plugins.Endpoints;

/// <summary>
/// One request the host forwards to a plugin after it matched a declared route and authorised the caller. It carries no
/// web framework type and no credential; <see cref="Host"/> and <see cref="Store"/> are bound to this call's client, so
/// no method on them takes a client id.
/// </summary>
public sealed class PluginCall
{
    /// <summary>Backing field of <see cref="RouteValues"/>; empty unless the route has parameters.</summary>
    private readonly IReadOnlyDictionary<string, string> _routeValues = new Dictionary<string, string>();

    /// <summary>Backing field of <see cref="Query"/>; empty unless the request has a query string.</summary>
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _query = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Backing field of <see cref="Context"/>; empty unless the call comes from a slot.</summary>
    private readonly IReadOnlyDictionary<string, string> _context = new Dictionary<string, string>();

    /// <summary>Gets the panel the call comes from.</summary>
    public required PluginArea Area { get; init; }

    /// <summary>Gets the HTTP method of the request.</summary>
    public required PluginRouteMethod Method { get; init; }

    /// <summary>Gets the declared route template that matched, for example <c>/invoices/{id}</c>.</summary>
    public required string Route { get; init; }

    /// <summary>Gets the language the caller asked for, a two-letter code such as <c>en</c>.</summary>
    public required string Language { get; init; }

    /// <summary>Gets who is calling.</summary>
    public required PluginCaller Caller { get; init; }

    /// <summary>
    /// Gets the read-only host facts about the call's client. Valid for the call, and for a streamed reply until the
    /// stream ends (see <see cref="PluginReply.Stream"/>).
    /// </summary>
    public required IPluginHostApi Host { get; init; }

    /// <summary>
    /// Gets the plugin's own data store, scoped to this plugin and the call's client. It is plugin wide only on an
    /// admin call that has no client; the host refuses a client area call without a client. Valid for the call, and
    /// for a streamed reply until the stream ends (see <see cref="PluginReply.Stream"/>).
    /// </summary>
    public required IPluginStore Store { get; init; }

    /// <summary>Gets the values of the route parameters, by name.</summary>
    public IReadOnlyDictionary<string, string> RouteValues
    {
        get => _routeValues;
        init => _routeValues = value ?? new Dictionary<string, string>();
    }

    /// <summary>Gets the query string values, by name; a repeated name keeps every value in order.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Query
    {
        get => _query;
        init => _query = value ?? new Dictionary<string, IReadOnlyList<string>>();
    }

    /// <summary>
    /// Gets the slot context the host verified for the caller, restricted to the context keys the widget declared, for
    /// example <c>serviceId</c>. The values are host ids. In the client area they name only the caller's own records;
    /// in the admin area they name the client and service the admin page shows. Empty on a call that does not come
    /// from a slot.
    /// </summary>
    public IReadOnlyDictionary<string, string> Context
    {
        get => _context;
        init => _context = value ?? new Dictionary<string, string>();
    }

    /// <summary>Gets the request body, already within the host's size cap; empty when there is none.</summary>
    public ReadOnlyMemory<byte> Body { get; init; }

    /// <summary>Gets the media type of <see cref="Body"/>, or <see langword="null"/> when there is no body.</summary>
    public string? ContentType { get; init; }
}
