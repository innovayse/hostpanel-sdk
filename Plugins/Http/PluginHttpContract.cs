using System.Net;
using System.Net.Sockets;
using System.Text;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.Http;

/// <summary>
/// Checks a plugin author runs in the plugin's own tests to prove that the HTTP client a plugin works with keeps the
/// redirect rule of the host: it does not follow redirects. Each check throws
/// <see cref="PluginContractViolationException"/> naming the promise that was broken. Nothing here depends on a test
/// framework. The check talks to a listener on the loopback address of its own, so the client under test must be able to
/// reach it: use the client of a test double of the host's transport, not a client behind the host's address guard.
/// </summary>
public static class PluginHttpContract
{
    /// <summary>The path the listener answers with a redirect.</summary>
    private const string FirstPath = "/first";

    /// <summary>
    /// Sends a request to a local listener that answers a redirect and asserts that the client returns the 3xx answer
    /// as it is and makes no second request. A client that follows redirects would send the same headers, and for some
    /// handlers the same body, to the address the answer names, which may be another host or an unintended path.
    /// </summary>
    /// <param name="createClient">Creates the client under test, for example <c>transport.CreateClient</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="createClient"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">
    /// The client followed the redirect, or did not return the redirect answer.
    /// </exception>
    public static async Task AssertRedirectIsNotFollowedAsync(Func<HttpClient> createClient, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(createClient);

        var requests = 0;
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var loop = Task.Run(() => ServeAsync(listener, () => Interlocked.Increment(ref requests), stop.Token), CancellationToken.None);
        try
        {
            using var client = createClient();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync($"http://127.0.0.1:{port}{FirstPath}", ct);
            }
            catch (HttpRequestException)
            {
                throw new PluginContractViolationException(
                    "The client failed on a redirect answer; it must return the 3xx answer as it is. A client that follows redirects can fail this way when they loop.");
            }

            using (response)
            {
                if (response.StatusCode != HttpStatusCode.Found)
                {
                    throw new PluginContractViolationException(
                        $"The client returned {(int)response.StatusCode} for a redirect answer of 302; it followed the redirect or changed the answer. It must return the 3xx answer as it is.");
                }
            }

            if (Volatile.Read(ref requests) != 1)
            {
                throw new PluginContractViolationException(
                    $"The client made {Volatile.Read(ref requests)} requests for one redirect answer; it must make exactly one and leave the next request to the plugin.");
            }
        }
        finally
        {
            await stop.CancelAsync();
            listener.Stop();
            await loop;
        }
    }

    /// <summary>Accepts connections until stopped and answers each: a 302 for the first path, a 200 for any other.</summary>
    /// <param name="listener">The listener to accept on.</param>
    /// <param name="counted">Called once for each request that arrives.</param>
    /// <param name="ct">Stops the loop.</param>
    /// <returns>A task that completes when the listener is stopped.</returns>
    private static async Task ServeAsync(TcpListener listener, Action counted, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcpClient connection;
            try
            {
                connection = await listener.AcceptTcpClientAsync(ct);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                return;
            }

            _ = Task.Run(() => AnswerAsync(connection, counted, ct), CancellationToken.None);
        }
    }

    /// <summary>Reads one request and answers it.</summary>
    /// <param name="connection">The accepted connection.</param>
    /// <param name="counted">Called once for the request.</param>
    /// <param name="ct">Stops the read.</param>
    /// <returns>A task that completes when the connection is closed.</returns>
    private static async Task AnswerAsync(TcpClient connection, Action counted, CancellationToken ct)
    {
        using (connection)
        {
            try
            {
                var stream = connection.GetStream();
                var seen = new StringBuilder();
                var buffer = new byte[1024];
                while (!seen.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
                {
                    var read = await stream.ReadAsync(buffer, ct);
                    if (read == 0)
                    {
                        return;
                    }

                    seen.Append(Encoding.ASCII.GetString(buffer, 0, read));
                }

                counted();
                var first = seen.ToString().StartsWith($"GET {FirstPath} ", StringComparison.Ordinal);
                var response = first
                    ? "HTTP/1.1 302 Found\r\nLocation: /second\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"
                    : "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                await stream.WriteAsync(Encoding.ASCII.GetBytes(response), ct);
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException)
            {
                // The client went away or the check ended; there is nothing left to answer.
            }
        }
    }
}
