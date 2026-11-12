using System.Net;
using System.Text;
using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.Http;

/// <summary>
/// Checks a plugin author runs in the plugin's own tests to prove that the HTTP client a plugin works with keeps the
/// redirect rule that a host of 3.7.0 or later must meet: it does not follow redirects. Each check throws
/// <see cref="PluginContractViolationException"/> naming the promise that was broken. Nothing here depends on a test
/// framework. The check talks to a listener on the loopback address of its own, so the client under test must be able
/// to reach it: use the client of a test double of the host's transport, not a client behind the host's address guard,
/// which may refuse a loopback address.
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
    /// The client followed the redirect, did not return the redirect answer, or could not reach the listener at all,
    /// in which case the check says nothing about redirects.
    /// </exception>
    public static async Task AssertRedirectIsNotFollowedAsync(
        Func<HttpClient> createClient,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(createClient);

        var requests = 0;
        Action counted = () => Interlocked.Increment(ref requests);
        await using var listener = new LoopbackListener((stream, token) => ServeAsync(stream, token, counted));
        using var client = createClient();

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"http://{IPAddress.Loopback}:{listener.Port}{FirstPath}", ct);
        }
        catch (HttpRequestException)
        {
            // A client that follows a redirect that loops fails with this too, but then it has made more than one
            // request; with one or none the listener was never reached and nothing was learned about redirects.
            throw new PluginContractViolationException(
                Volatile.Read(ref requests) > 1
                    ? "The client followed the redirect and kept following it; it must return the 3xx answer as it is."
                    : "The client could not reach the local listener (the address may be blocked), so the check says " +
                      "nothing about redirects.");
        }

        using (response)
        {
            if (response.StatusCode != HttpStatusCode.Found)
            {
                throw new PluginContractViolationException(
                    $"The client returned {(int)response.StatusCode} for a redirect answer of 302; it followed the " +
                    "redirect or changed the answer. It must return the 3xx answer as it is.");
            }
        }

        var made = Volatile.Read(ref requests);
        if (made != 1)
        {
            throw new PluginContractViolationException(
                $"The client made {made} requests for one redirect answer; it must make exactly one and leave the " +
                "next request to the plugin.");
        }
    }

    /// <summary>Answers one request: a 302 for the first path, a 200 for any other.</summary>
    /// <param name="stream">The connection.</param>
    /// <param name="ct">Stops the read.</param>
    /// <param name="counted">Called once for the request.</param>
    /// <returns>A task that completes when the answer is written.</returns>
    private static async Task ServeAsync(Stream stream, CancellationToken ct, Action counted)
    {
        var head = await LoopbackListener.ReadHeadAsync(stream, ct);
        if (head is null)
        {
            return;
        }

        counted();
        var response = head.StartsWith($"GET {FirstPath} ", StringComparison.Ordinal)
            ? "HTTP/1.1 302 Found\r\nLocation: /second\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"
            : "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(response), ct);
    }
}
