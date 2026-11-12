using System.Net;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Endpoints;

/// <summary>
/// What a plugin answers to a <see cref="PluginCall"/>, without any web framework type: a JSON document, no content,
/// a problem, a stream of events, a raw body or a redirect. Build one with the factory methods; the host turns it into
/// the response.
/// </summary>
public sealed class PluginReply
{
    /// <summary>200, the default status of a JSON reply and the status of a stream.</summary>
    private const int StatusOk = (int)HttpStatusCode.OK;

    /// <summary>201, a JSON reply that created something.</summary>
    private const int StatusCreated = (int)HttpStatusCode.Created;

    /// <summary>202, a JSON reply that accepted work not yet done.</summary>
    private const int StatusAccepted = (int)HttpStatusCode.Accepted;

    /// <summary>203, a JSON reply whose content comes from a copy.</summary>
    private const int StatusNonAuthoritative = (int)HttpStatusCode.NonAuthoritativeInformation;

    /// <summary>204, the status of a reply without content.</summary>
    private const int StatusNoContent = (int)HttpStatusCode.NoContent;

    /// <summary>303, the default status of a redirect: the browser follows it with a plain GET.</summary>
    private const int StatusSeeOther = (int)HttpStatusCode.SeeOther;

    /// <summary>401, an authentication challenge the host answers itself.</summary>
    private const int StatusUnauthorized = (int)HttpStatusCode.Unauthorized;

    /// <summary>403, the status a plugin answers to refuse a caller.</summary>
    private const int StatusForbidden = (int)HttpStatusCode.Forbidden;

    /// <summary>407, a proxy authentication challenge the host answers itself.</summary>
    private const int StatusProxyAuthenticationRequired = (int)HttpStatusCode.ProxyAuthenticationRequired;

    /// <summary>The lowest status of any answer a plugin gives.</summary>
    private const int FirstAnswerStatus = StatusOk;

    /// <summary>The lowest status of a failure.</summary>
    private const int FirstFailureStatus = (int)HttpStatusCode.BadRequest;

    /// <summary>
    /// The highest status of any answer a plugin gives. It stays a literal: <see cref="HttpStatusCode"/> names no member
    /// for the end of the 5xx class, which is only a limit of the class and not a status.
    /// </summary>
    private const int LastAnswerStatus = 599;

    /// <summary>The five redirect statuses a reply may use: permanent, found, see other, temporary and permanent keeping the method.</summary>
    private static readonly int[] RedirectStatuses =
    [
        (int)HttpStatusCode.MovedPermanently,
        (int)HttpStatusCode.Found,
        (int)HttpStatusCode.SeeOther,
        (int)HttpStatusCode.TemporaryRedirect,
        (int)HttpStatusCode.PermanentRedirect,
    ];

    /// <summary>The end of the message of a refused status that the host would answer itself, so a problem and a raw reply say it the same way.</summary>
    private static readonly string HostChallengeAdvice =
        $"other than {StatusUnauthorized} and {StatusProxyAuthenticationRequired}; answer {StatusForbidden} to refuse a caller.";

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

    /// <summary>Gets the bytes written to the response as is; set for <see cref="PluginReplyKind.Raw"/> only, empty otherwise.</summary>
    public ReadOnlyMemory<byte> RawBody { get; private init; }

    /// <summary>Gets the media type of <see cref="RawBody"/>, for example <c>text/plain</c>; set for <see cref="PluginReplyKind.Raw"/> only.</summary>
    public string? ContentType { get; private init; }

    /// <summary>
    /// Gets the address the host sends the caller on to; set for <see cref="PluginReplyKind.Redirect"/> only.
    /// </summary>
    public string? Location { get; private init; }

    /// <summary>Builds a JSON reply.</summary>
    /// <param name="body">The value to serialise.</param>
    /// <param name="status">
    /// One of 200, 201, 202 or 203; 200 by default. 204 has no body (use <see cref="NoContent"/>), and the other 2xx
    /// statuses either have no body or are not an answer to a call.
    /// </param>
    /// <returns>The reply.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="status"/> is not a success status with a body.</exception>
    public static PluginReply Json(object body, int status = StatusOk)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (status is not (StatusOk or StatusCreated or StatusAccepted or StatusNonAuthoritative))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status), status, $"A JSON reply needs a status of {StatusOk}, {StatusCreated}, {StatusAccepted} or {StatusNonAuthoritative}.");
        }

        return new PluginReply(PluginReplyKind.Json, status) { Body = body };
    }

    /// <summary>Builds an empty success reply (204).</summary>
    /// <returns>The reply.</returns>
    public static PluginReply NoContent()
    {
        return new PluginReply(PluginReplyKind.NoContent, StatusNoContent);
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
        if (status is < FirstFailureStatus or > LastAnswerStatus || IsAnsweredByHost(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status), status, $"A problem needs a 4xx or 5xx status {HostChallengeAdvice}");
        }

        return new PluginReply(PluginReplyKind.Problem, status) { Code = code, Detail = detail };
    }

    /// <summary>
    /// Builds a raw reply: the bytes are written to the response unchanged, with the given status and media type.
    /// Use it where the caller expects a fixed answer that is not JSON, such as the acknowledgement a gateway waits for
    /// after a callback. The host may still refuse a body over its size cap.
    /// </summary>
    /// <param name="status">
    /// Any status from 200 to 599 except 401 and 407. Those two are authentication challenges the host answers itself
    /// (see <see cref="Problem"/>), so a plugin that refuses a caller answers 403.
    /// </param>
    /// <param name="body">The bytes to send; may be empty.</param>
    /// <param name="contentType">The media type, for example <c>text/plain; charset=utf-8</c>; it must not contain a line break.</param>
    /// <returns>The reply.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contentType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="contentType"/> is empty, white space or contains a line break.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="status"/> is outside 200 to 599, or is 401 or 407.</exception>
    public static PluginReply Raw(int status, ReadOnlyMemory<byte> body, string contentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (status is < FirstAnswerStatus or > LastAnswerStatus || IsAnsweredByHost(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status), status, $"A raw reply needs a status from {FirstAnswerStatus} to {LastAnswerStatus} {HostChallengeAdvice}");
        }

        if (contentType.Contains('\r') || contentType.Contains('\n'))
        {
            throw new ArgumentException("A media type must not contain a line break.", nameof(contentType));
        }

        return new PluginReply(PluginReplyKind.Raw, status) { RawBody = body, ContentType = contentType };
    }

    /// <summary>
    /// Builds a redirect: the host answers with this status and a <c>Location</c> header. A plugin never writes the
    /// header itself. Use it on a public callback route, for example to send the payer from the return address of a
    /// payment on to the host page that shows the result. Never build the location from data of the request, such as a
    /// query parameter: an address the caller chose is an open redirect.
    /// </summary>
    /// <param name="location">
    /// Where to send the caller: either a host relative path (one leading slash, never two, no backslash, and never
    /// under the API path, which the host checks again) or an absolute <c>https</c> URL without user info. The address
    /// has no control character (a line break, a tab, a null, DEL or a C1 control), no leading or trailing white space,
    /// and no backslash. The host validates the address again before it answers, and a plugin that breaks a rule is a
    /// plugin fault, answered as any other bad reply. The kind needs a host of 3.7.0 or later.
    /// </param>
    /// <param name="status">One of 301, 302, 303, 307 or 308; 303 (see other) by default.</param>
    /// <returns>The reply.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="location"/> is empty, white space or not an accepted address.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="status"/> is not a redirect status.</exception>
    public static PluginReply Redirect(string location, int status = StatusSeeOther)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(location);
        if (!RedirectStatuses.Contains(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status), status, $"A redirect needs a status of {string.Join(", ", RedirectStatuses.SkipLast(1))} or {RedirectStatuses[^1]}.");
        }

        if (!IsAcceptedLocation(location))
        {
            throw new ArgumentException(
                "A redirect goes to a host relative path or an absolute https URL without user info, with no " +
                "control character, no surrounding white space and no backslash.",
                nameof(location));
        }

        return new PluginReply(PluginReplyKind.Redirect, status) { Location = location };
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
        return new PluginReply(PluginReplyKind.Stream, StatusOk) { Events = events };
    }

    /// <summary>Tells whether an address is one a redirect may go to.</summary>
    /// <param name="location">The address to test.</param>
    /// <returns><see langword="true"/> for a host relative path or an absolute https URL without user info.</returns>
    private static bool IsAcceptedLocation(string location)
    {
        // A browser strips a tab or a line break inside an address, so "/<tab>/host" would read as "//host".
        if (location.Any(char.IsControl) || location.Contains('\\') || location.Trim() != location)
        {
            return false;
        }

        if (location.StartsWith('/'))
        {
            return !location.StartsWith("//", StringComparison.Ordinal);
        }

        return Uri.TryCreate(location, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.UserInfo.Length == 0;
    }

    /// <summary>Tells whether the host answers a status itself: the authentication challenges that would sign a client out or ask for proxy credentials.</summary>
    /// <param name="status">The status a plugin asked for.</param>
    /// <returns><see langword="true"/> for 401 and 407.</returns>
    private static bool IsAnsweredByHost(int status)
    {
        return status is StatusUnauthorized or StatusProxyAuthenticationRequired;
    }
}
