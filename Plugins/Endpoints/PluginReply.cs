using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.Endpoints;

/// <summary>
/// What a plugin answers to a <see cref="PluginCall"/>, without any web framework type: a JSON document, no content,
/// a problem or a stream of events. Build one with the factory methods; the host turns it into the response.
/// </summary>
public sealed class PluginReply
{
    /// <summary>Initializes a new instance of the <see cref="PluginReply"/> class.</summary>
    /// <param name="kind">The shape of the reply.</param>
    /// <param name="status">The HTTP status the host answers with.</param>
    private PluginReply(PluginReplyKind kind, int status)
    {
        Kind = kind;
        Status = status;
    }

    /// <summary>Gets the shape of the reply.</summary>
    public PluginReplyKind Kind { get; }

    /// <summary>Gets the HTTP status the host answers with.</summary>
    public int Status { get; }

    /// <summary>Gets the value the host serialises as camelCase JSON; set for <see cref="PluginReplyKind.Json"/> only.</summary>
    public object? Body { get; private init; }

    /// <summary>Gets the stable machine-readable error code; set for <see cref="PluginReplyKind.Problem"/> only.</summary>
    public string? Code { get; private init; }

    /// <summary>Gets the short explanation of a problem, or <see langword="null"/>; never put secrets or stack traces here.</summary>
    public string? Detail { get; private init; }

    /// <summary>Gets the events to forward; set for <see cref="PluginReplyKind.Stream"/> only.</summary>
    public IAsyncEnumerable<PluginEvent>? Events { get; private init; }

    /// <summary>Builds a JSON reply.</summary>
    /// <param name="body">The value to serialise.</param>
    /// <param name="status">
    /// One of 200, 201, 202 or 203; 200 by default. 204 has no body (use <see cref="NoContent"/>), and the other 2xx
    /// statuses either have no body or are not an answer to a call.
    /// </param>
    /// <returns>The reply.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="status"/> is not a success status with a body.</exception>
    public static PluginReply Json(object body, int status = 200)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (status is not (200 or 201 or 202 or 203))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "A JSON reply needs a status of 200, 201, 202 or 203.");
        }

        return new PluginReply(PluginReplyKind.Json, status) { Body = body };
    }

    /// <summary>Builds an empty success reply (204).</summary>
    /// <returns>The reply.</returns>
    public static PluginReply NoContent()
    {
        return new PluginReply(PluginReplyKind.NoContent, 204);
    }

    /// <summary>Builds a problem; the host maps it to its standard error response.</summary>
    /// <param name="status">
    /// A failure status from 400 to 599 except 401 and 407. Those two are authentication challenges the host answers
    /// itself (they would make a client sign out or ask for proxy credentials), so a plugin that refuses a caller
    /// answers 403.
    /// </param>
    /// <param name="code">The stable error code, for example <c>invoice_not_found</c>.</param>
    /// <param name="detail">A short explanation, or <see langword="null"/>.</param>
    /// <returns>The reply.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="code"/> is empty or white space.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="status"/> is not a failure status.</exception>
    public static PluginReply Problem(int status, string code, string? detail = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (status is < 400 or > 599 or 401 or 407)
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "A problem needs a 4xx or 5xx status other than 401 and 407; answer 403 to refuse a caller.");
        }

        return new PluginReply(PluginReplyKind.Problem, status) { Code = code, Detail = detail };
    }

    /// <summary>
    /// Builds a streamed reply, forwarded as server-sent events.
    /// <para>
    /// Cancellation: the host enumerates <paramref name="events"/> after <see cref="IPluginEndpoints.HandleAsync"/> has
    /// returned, so the token given to <c>HandleAsync</c> is not used for the stream and must not be captured by it.
    /// When the caller disconnects, the host cancels the token it passes to <c>GetAsyncEnumerator</c>. An iterator
    /// method receives that token only through a parameter marked
    /// <see cref="System.Runtime.CompilerServices.EnumeratorCancellationAttribute"/>; without the attribute the stream
    /// never learns that the caller left. Pass that token to every awaited call inside the iterator.
    /// </para>
    /// <para>
    /// Lifetime: <see cref="PluginCall.Host"/> and <see cref="PluginCall.Store"/> stay valid until the stream ends
    /// (completes, throws or is cancelled), so an iterator may keep using them. The host disposes them after that, and
    /// a plugin must not use them later.
    /// </para>
    /// </summary>
    /// <param name="events">The events, produced as they become available.</param>
    /// <returns>The reply.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="events"/> is <see langword="null"/>.</exception>
    public static PluginReply Stream(IAsyncEnumerable<PluginEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        return new PluginReply(PluginReplyKind.Stream, 200) { Events = events };
    }
}
