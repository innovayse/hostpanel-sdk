using System.Net.Security;
using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A transport that trusts a self signed certificate the way the contract describes (for one server, on request) or, with
/// <see cref="TrustEveryRequest"/>, the way it must not (for every request), so the contract helper can be shown to
/// accept the first and reject the second.
/// </summary>
internal sealed class SelfSignedTransport : IPanelTransport
{
    /// <summary>Holds the ids of the servers the operator allowed a self signed certificate for.</summary>
    private readonly HashSet<int> _allowed;

    /// <summary>Initializes a new instance of the <see cref="SelfSignedTransport"/> class.</summary>
    /// <param name="allowedServerIds">The ids of the servers the operator allowed a self signed certificate for.</param>
    public SelfSignedTransport(params int[] allowedServerIds)
    {
        _allowed = [.. allowedServerIds];
    }

    /// <summary>Gets a value indicating whether every client trusts every certificate, which the contract forbids.</summary>
    public bool TrustEveryRequest { get; init; }

    /// <summary>Gets a value indicating whether a trusted server is trusted on every port, which the contract forbids.</summary>
    public bool TrustAnyPort { get; init; }

    /// <summary>Gets a value indicating whether a trusted server is trusted under every name, which the contract forbids.</summary>
    public bool TrustAnyName { get; init; }

    /// <inheritdoc />
    public HttpClient CreateClient()
    {
        return TrustEveryRequest ? Trusting(_ => true) : new HttpClient();
    }

    /// <inheritdoc />
    public HttpClient CreateClient(PanelServerConnection server)
    {
        // Trust one host name and port only: the certificate of any other address is still refused.
        return _allowed.Contains(server.ServerId)
            ? Trusting(uri => (TrustAnyName || uri.Host == server.Hostname) && (TrustAnyPort || uri.Port == server.Port))
            : CreateClient();
    }

    /// <summary>Builds a client that accepts any certificate for the addresses the filter allows.</summary>
    /// <param name="allowed">Tells whether the address of a request may use a self signed certificate.</param>
    /// <returns>The client.</returns>
    private static HttpClient Trusting(Func<Uri, bool> allowed)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, _, _, errors) =>
                errors == SslPolicyErrors.None || (message.RequestUri is { } uri && allowed(uri)),
        };
        return new HttpClient(handler);
    }
}
