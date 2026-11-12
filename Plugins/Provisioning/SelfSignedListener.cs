using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Hostpanel.Sdk.Plugins.Diagnostics;

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

    /// <summary>The loopback address the listener answers on, a second name for the same endpoint.</summary>
    public const string LoopbackAddress = "127.0.0.1";

    /// <summary>The certificate the listener presents.</summary>
    private readonly X509Certificate2 _certificate;

    /// <summary>The loopback server that accepts the connections.</summary>
    private readonly LoopbackListener _inner;

    /// <summary>Initializes a new instance of the <see cref="SelfSignedListener"/> class and starts listening.</summary>
    private SelfSignedListener()
    {
        _certificate = CreateCertificate();
        _inner = new LoopbackListener(AnswerAsync, _certificate);
    }

    /// <summary>Gets the port the listener answers on.</summary>
    public int Port => _inner.Port;

    /// <summary>Starts a listener.</summary>
    /// <returns>The running listener; dispose it when the test ends.</returns>
    public static SelfSignedListener Start()
    {
        return new SelfSignedListener();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _inner.DisposeAsync();
        _certificate.Dispose();
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

    /// <summary>Answers one connection with <c>200 ok</c> once the request headers have arrived.</summary>
    /// <param name="stream">The encrypted connection.</param>
    /// <param name="ct">Stops the read when the listener is disposed.</param>
    /// <returns>A task that completes when the answer is written.</returns>
    private static async Task AnswerAsync(Stream stream, CancellationToken ct)
    {
        if (await LoopbackListener.ReadHeadAsync(stream, ct) is null)
        {
            return;
        }

        var response = "HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nok"u8.ToArray();
        await stream.WriteAsync(response, ct);
        await stream.FlushAsync(ct);
    }
}
