using System.Security.Authentication;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Http;

/// <summary>
/// Small helpers for the HTTP code every plugin that talks to a gateway or a panel writes: send a request with a
/// deadline and turn a transport fault into one exception without the address, check where the answer came from, and
/// read a body with a size limit and a deadline. Using them keeps the steps the same in every plugin, so an unbounded
/// read, a body that never ends or a leaked address is not left to each author. They add no dependency beyond the
/// framework and work on any <see cref="HttpClient"/>, including the one from the host's transport. Every failure is a
/// <see cref="PluginHttpException"/> with a fixed message, except a cancellation the caller asked for, which stays an
/// <see cref="OperationCanceledException"/>. The clock is injected as a <see cref="TimeProvider"/>, so a test moves it
/// instead of waiting.
/// </summary>
public sealed class PluginHttp
{
    /// <summary>The size of the blocks the body is read in.</summary>
    private const int BlockSize = 8192;

    /// <summary>The fixed sentence of a transport fault; it holds no address, query or header.</summary>
    private const string TransportMessage =
        "The request did not complete: a refused or dropped connection, or a TLS error.";

    /// <summary>The clock the deadlines run on.</summary>
    private readonly TimeProvider _time;

    /// <summary>Initializes a new instance of the <see cref="PluginHttp"/> class.</summary>
    /// <param name="timeProvider">The clock the deadlines run on; <see cref="TimeProvider.System"/> in
    /// production.</param>
    /// <exception cref="ArgumentNullException"><paramref name="timeProvider"/> is <see langword="null"/>.</exception>
    public PluginHttp(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _time = timeProvider;
    }

    /// <summary>
    /// Sends a request and returns as soon as the response headers have arrived, so the body can be refused before it
    /// is read (see <see cref="ReadBoundedAsync"/>). The call is bounded by <paramref name="timeout"/>. The caller
    /// disposes the response.
    /// </summary>
    /// <param name="client">The client to send on, normally the one the host's transport gave the plugin.</param>
    /// <param name="request">The request to send.</param>
    /// <param name="timeout">How long the call may take; longer than zero.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The response, with its body not yet read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="request"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is zero or negative.</exception>
    /// <exception cref="PluginHttpException">
    /// The request timed out (<see cref="PluginHttpFault.TimedOut"/>) or did not complete
    /// (<see cref="PluginHttpFault.Transport"/>), with no inner exception, because the inner message can contain the
    /// request address and query.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    public Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpRequestMessage request,
        TimeSpan timeout,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        return RunAsync(
            token => client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token), timeout, ct);
    }

    /// <summary>
    /// Asserts that a response came from the address the plugin built the request for: the same scheme, host and port.
    /// A client that follows redirects ends on the address of the last hop, so this is the check that a credential was
    /// not sent on to another host, another port or plain HTTP. Hosts compare without regard to case, and an IPv6
    /// address is compared with its brackets, as <see cref="Uri.Host"/> writes it, on both sides, so pass the
    /// <see cref="Uri"/> and not a host text.
    /// </summary>
    /// <param name="response">The response to check.</param>
    /// <param name="expected">The address the request was built for; only its scheme, host and port are
    /// compared.</param>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> or <paramref name="expected"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="expected"/> is not an absolute address.</exception>
    /// <exception cref="PluginHttpException">
    /// The response came from another scheme, host or port, or from a request without an address.
    /// </exception>
    public static void EnsureFinalHost(HttpResponseMessage response, Uri expected)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(expected);
        if (!expected.IsAbsoluteUri)
        {
            throw new ArgumentException("The expected address must be absolute.", nameof(expected));
        }

        var actual = response.RequestMessage?.RequestUri;
        if (actual is null
            || !actual.IsAbsoluteUri
            || !string.Equals(actual.Scheme, expected.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(actual.Host, expected.Host, StringComparison.OrdinalIgnoreCase)
            || actual.Port != expected.Port)
        {
            throw new PluginHttpException(
                PluginHttpFault.UnexpectedHost,
                "The response came from another scheme, host or port than the one the request was built for.");
        }
    }

    /// <summary>
    /// Reads the body of a response with a size limit and a deadline. A declared <c>Content-Length</c> over the limit
    /// is refused before a byte is read; a body with no declared length is read up to the limit and refused when it
    /// goes on; a body that arrives too slowly is cut off when the deadline passes.
    /// </summary>
    /// <param name="response">The response to read.</param>
    /// <param name="maxBytes">The most bytes accepted; at least 1.</param>
    /// <param name="timeout">How long the read may take; longer than zero.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The body, at most <paramref name="maxBytes"/> long; empty when the response has no content.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxBytes"/> is less than 1, or <paramref name="timeout"/> is zero or negative.
    /// </exception>
    /// <exception cref="PluginHttpException">
    /// The body is larger than the limit (<see cref="PluginHttpFault.TooLarge"/>), the deadline passed
    /// (<see cref="PluginHttpFault.TimedOut"/>), or the connection failed while reading
    /// (<see cref="PluginHttpFault.Transport"/>).
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    public Task<byte[]> ReadBoundedAsync(
        HttpResponseMessage response,
        int maxBytes,
        TimeSpan timeout,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBytes, 1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        if (response.Content.Headers.ContentLength is { } declared && declared > maxBytes)
        {
            return Task.FromException<byte[]>(TooLarge());
        }

        return RunAsync(token => ReadAsync(response, maxBytes, token), timeout, ct);
    }

    /// <summary>Reads a body in blocks and stops with <see cref="PluginHttpFault.TooLarge"/> when it passes the
    /// limit.</summary>
    /// <param name="response">The response to read.</param>
    /// <param name="maxBytes">The most bytes accepted.</param>
    /// <param name="ct">Stops the read when the deadline passes or the caller cancels.</param>
    /// <returns>The body.</returns>
    private static async Task<byte[]> ReadAsync(HttpResponseMessage response, int maxBytes, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var body = new MemoryStream();
        var block = new byte[BlockSize];
        int read;
        while ((read = await stream.ReadAsync(block, ct)) > 0)
        {
            if (body.Length + read > maxBytes)
            {
                throw TooLarge();
            }

            body.Write(block, 0, read);
        }

        return body.ToArray();
    }

    /// <summary>Builds the exception for a body over the limit.</summary>
    /// <returns>The exception, with a fixed message.</returns>
    private static PluginHttpException TooLarge()
    {
        return new PluginHttpException(PluginHttpFault.TooLarge, "The response body is larger than the limit.");
    }

    /// <summary>
    /// Runs one operation under the deadline and maps what goes wrong: a deadline that passes, or a cancellation nobody
    /// asked for, is <see cref="PluginHttpFault.TimedOut"/>; a transport fault is
    /// <see cref="PluginHttpFault.Transport"/> with no inner exception; a cancellation by the caller is left alone.
    /// </summary>
    /// <typeparam name="T">The result of the operation.</typeparam>
    /// <param name="operation">The operation, which must stop when the token it is given is cancelled.</param>
    /// <param name="timeout">How long the operation may take; longer than zero.</param>
    /// <param name="ct">The caller's token.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is zero or negative.</exception>
    private async Task<T> RunAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout,
        CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        using var deadline = new CancellationTokenSource(timeout, _time);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, deadline.Token);
        try
        {
            return await operation(linked.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or AuthenticationException
            || (ex is OperationCanceledException && !ct.IsCancellationRequested))
        {
            // Not rethrown as the inner exception: its message can carry the address and the query string.
            throw ex is OperationCanceledException
                ? new PluginHttpException(PluginHttpFault.TimedOut, "The request did not finish before its deadline.")
                : new PluginHttpException(PluginHttpFault.Transport, TransportMessage);
        }
    }
}
