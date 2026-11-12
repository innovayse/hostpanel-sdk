using System.Security.Authentication;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Http;

/// <summary>
/// Small helpers for the HTTP code every plugin that talks to a gateway or a panel writes: send a request and turn a
/// transport fault into one exception without the address, check where the answer came from, and read a body with a size
/// limit. Using them keeps the steps the same in every plugin, so an unbounded read or a leaked address is not left to
/// each author. They add no dependency beyond the framework and work on any <see cref="HttpClient"/>, including the one
/// from the host's transport. Every failure is a <see cref="PluginHttpException"/> with a fixed message.
/// </summary>
public static class PluginHttp
{
    /// <summary>The size of the blocks the body is read in.</summary>
    private const int BlockSize = 8192;

    /// <summary>
    /// Sends a request and returns as soon as the response headers have arrived, so the body can be refused before it is
    /// read (see <see cref="ReadBoundedAsync"/>). A timeout, a refused or dropped connection and a TLS error become a
    /// <see cref="PluginHttpException"/> with <see cref="PluginHttpFault.Transport"/> and no inner exception, because the
    /// inner message can contain the request address and query. A cancelled <paramref name="ct"/> still throws
    /// <see cref="OperationCanceledException"/>. The caller disposes the response.
    /// </summary>
    /// <param name="client">The client to send on, normally the one the host's transport gave the plugin.</param>
    /// <param name="request">The request to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The response, with its body not yet read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginHttpException">The request did not complete.</exception>
    public static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or AuthenticationException
            || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            // Not rethrown as the inner exception: its message can carry the address and the query string.
            throw new PluginHttpException(
                PluginHttpFault.Transport,
                "The request did not complete: a timeout, a refused or dropped connection, or a TLS error.");
        }
    }

    /// <summary>
    /// Asserts that a response came from the host the plugin built the request for. A client that follows redirects ends
    /// on the address of the last hop, so this is the check that a credential was not sent on to another host.
    /// </summary>
    /// <param name="response">The response to check.</param>
    /// <param name="expectedHost">The host name the plugin called, without a scheme or a port.</param>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="expectedHost"/> is empty or white space.</exception>
    /// <exception cref="PluginHttpException">The response came from another host, or from a request without an address.</exception>
    public static void EnsureFinalHost(HttpResponseMessage response, string expectedHost)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedHost);

        var host = response.RequestMessage?.RequestUri?.Host;
        if (!string.Equals(host, expectedHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginHttpException(
                PluginHttpFault.UnexpectedHost,
                "The response came from another host than the one the request was built for.");
        }
    }

    /// <summary>
    /// Reads the body of a response with a size limit. A declared <c>Content-Length</c> over the limit is refused before
    /// a byte is read; a body with no declared length is read up to the limit and refused when it goes on.
    /// </summary>
    /// <param name="response">The response to read.</param>
    /// <param name="maxBytes">The most bytes accepted; at least 1.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The body, at most <paramref name="maxBytes"/> long; empty when the response has no content.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxBytes"/> is less than 1.</exception>
    /// <exception cref="PluginHttpException">The body is larger than the limit, or the connection failed while reading it.</exception>
    public static async Task<byte[]> ReadBoundedAsync(HttpResponseMessage response, int maxBytes, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBytes, 1);

        if (response.Content.Headers.ContentLength is { } declared && declared > maxBytes)
        {
            throw TooLarge();
        }

        try
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
        catch (Exception ex) when (ex is HttpRequestException or IOException
            || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new PluginHttpException(
                PluginHttpFault.Transport,
                "The request did not complete: a timeout, a refused or dropped connection, or a TLS error.");
        }
    }

    /// <summary>Builds the exception for a body over the limit.</summary>
    /// <returns>The exception, with a fixed message.</returns>
    private static PluginHttpException TooLarge()
    {
        return new PluginHttpException(PluginHttpFault.TooLarge, "The response body is larger than the limit.");
    }
}
