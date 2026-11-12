using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Hostpanel.Sdk.Plugins.Diagnostics;

/// <summary>
/// A small server on the loopback address that the contract helpers of the SDK start for a test: it accepts
/// connections, optionally speaks TLS with a given certificate, and hands each connection to a method that answers it.
/// It is the one accept loop and the one bounded header read the helpers share, so a listener that serves a
/// self signed certificate and one that serves a redirect cannot drift apart. Internal: a plugin author uses the
/// helpers built on it.
/// </summary>
internal sealed class LoopbackListener : IAsyncDisposable
{
    /// <summary>The most bytes of request headers read before a connection is given up on.</summary>
    private const int MaxHeaderBytes = 16 * 1024;

    /// <summary>How many bytes of the request are read at a time.</summary>
    private const int ReadChunkBytes = 1024;

    /// <summary>The end of the headers of an HTTP request.</summary>
    private const string HeaderEnd = "\r\n\r\n";

    /// <summary>The method that answers one connection.</summary>
    private readonly Func<Stream, CancellationToken, Task> _serve;

    /// <summary>The certificate to speak TLS with, or <see langword="null"/> for plain TCP.</summary>
    private readonly X509Certificate2? _certificate;

    /// <summary>The socket that accepts connections.</summary>
    private readonly TcpListener _listener;

    /// <summary>Stops the accept loop and the open connections when the listener is disposed.</summary>
    private readonly CancellationTokenSource _stop = new();

    /// <summary>The accept loop.</summary>
    private readonly Task _loop;

    /// <summary>Initializes a new instance of the <see cref="LoopbackListener"/> class and starts listening.</summary>
    /// <param name="serve">Answers one connection, given its stream (already encrypted when there is a certificate).</param>
    /// <param name="certificate">The certificate to speak TLS with, or <see langword="null"/> for plain TCP.</param>
    internal LoopbackListener(Func<Stream, CancellationToken, Task> serve, X509Certificate2? certificate = null)
    {
        _serve = serve;
        _certificate = certificate;
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        _loop = Task.Run(AcceptAsync);
    }

    /// <summary>Gets the port the listener answers on.</summary>
    internal int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>
    /// Reads the head of an HTTP request: everything up to the empty line, never more than the limit.
    /// </summary>
    /// <param name="stream">The connection.</param>
    /// <param name="ct">Stops the read.</param>
    /// <returns>The head, or <see langword="null"/> when the client closed first or sent too much.</returns>
    internal static async Task<string?> ReadHeadAsync(Stream stream, CancellationToken ct)
    {
        var seen = new StringBuilder();
        var buffer = new byte[ReadChunkBytes];
        while (!seen.ToString().Contains(HeaderEnd, StringComparison.Ordinal))
        {
            var read = await stream.ReadAsync(buffer, ct);
            if (read == 0)
            {
                return null;
            }

            seen.Append(Encoding.ASCII.GetString(buffer, 0, read));
            if (seen.Length > MaxHeaderBytes)
            {
                return null;
            }
        }

        return seen.ToString();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        try
        {
            await _loop;
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
        {
            // The loop ends by its listener being stopped; nothing else is left to report.
        }

        _stop.Dispose();
    }

    /// <summary>Accepts connections until the listener is stopped, and answers each on its own task.</summary>
    /// <returns>A task that completes when the listener is stopped.</returns>
    private async Task AcceptAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_stop.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                return;
            }

            _ = Task.Run(() => AnswerAsync(client), CancellationToken.None);
        }
    }

    /// <summary>Answers one connection: the TLS handshake when there is a certificate, then the serve method.</summary>
    /// <param name="client">The accepted connection.</param>
    /// <returns>A task that completes when the connection is closed.</returns>
    private async Task AnswerAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                Stream stream = client.GetStream();
                if (_certificate is not null)
                {
                    var tls = new SslStream(stream, false);
                    await tls.AuthenticateAsServerAsync(_certificate);
                    stream = tls;
                }

                await using (stream)
                {
                    await _serve(stream, _stop.Token);
                }
            }
            catch (Exception ex) when (ex is IOException or AuthenticationException or OperationCanceledException
                or ObjectDisposedException)
            {
                // A client that refuses the certificate or goes away ends the connection with an error; that is the
                // case the tests want, and there is nothing left to answer.
            }
        }
    }
}
