using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// A local HTTPS listener with a self signed certificate for <c>localhost</c>, for the tests of a plugin author: it
/// answers every request with <c>200</c> and the text <c>ok</c>, so a client that refuses the certificate fails and a
/// client that accepts it for this server succeeds. It listens on the loopback address on a free port and has no
/// dependency beyond the framework. Dispose it to stop it.
/// </summary>
public sealed class SelfSignedListener : IAsyncDisposable
{
    /// <summary>The name the certificate is issued to, and the host name a test connects to.</summary>
    public const string HostName = "localhost";

    /// <summary>The certificate the listener presents.</summary>
    private readonly X509Certificate2 _certificate;

    /// <summary>The socket that accepts connections.</summary>
    private readonly TcpListener _listener;

    /// <summary>Stops the accept loop when the listener is disposed.</summary>
    private readonly CancellationTokenSource _stop = new();

    /// <summary>The accept loop.</summary>
    private readonly Task _loop;

    /// <summary>Initializes a new instance of the <see cref="SelfSignedListener"/> class and starts listening.</summary>
    private SelfSignedListener()
    {
        _certificate = CreateCertificate();
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        _loop = Task.Run(AcceptAsync);
    }

    /// <summary>Gets the port the listener answers on.</summary>
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>Starts a listener.</summary>
    /// <returns>The running listener; dispose it when the test ends.</returns>
    public static SelfSignedListener Start()
    {
        return new SelfSignedListener();
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

        _certificate.Dispose();
        _stop.Dispose();
    }

    /// <summary>Creates a self signed certificate for <see cref="HostName"/> whose private key a TLS stack can use.</summary>
    /// <returns>The certificate.</returns>
    private static X509Certificate2 CreateCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={HostName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName(HostName);
        names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
        using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        // Exporting and importing makes the private key persistent, which some platforms need to serve TLS with it.
        return new X509Certificate2(created.Export(X509ContentType.Pfx));
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

            _ = Task.Run(() => AnswerAsync(client));
        }
    }

    /// <summary>Answers one connection: the TLS handshake, then <c>200 ok</c> once the request headers have arrived.</summary>
    /// <param name="client">The accepted connection.</param>
    /// <returns>A task that completes when the connection is closed.</returns>
    private async Task AnswerAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                await using var tls = new SslStream(client.GetStream(), false);
                await tls.AuthenticateAsServerAsync(_certificate);

                var seen = new StringBuilder();
                var buffer = new byte[1024];
                while (!seen.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
                {
                    var read = await tls.ReadAsync(buffer, _stop.Token);
                    if (read == 0)
                    {
                        return;
                    }

                    seen.Append(Encoding.ASCII.GetString(buffer, 0, read));
                }

                var response = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok"u8.ToArray();
                await tls.WriteAsync(response, _stop.Token);
                await tls.FlushAsync(_stop.Token);
            }
            catch (Exception ex) when (ex is IOException or System.Security.Authentication.AuthenticationException
                or OperationCanceledException or ObjectDisposedException)
            {
                // A client that refuses the certificate ends the handshake with an error; that is the case the tests want.
            }
        }
    }
}
