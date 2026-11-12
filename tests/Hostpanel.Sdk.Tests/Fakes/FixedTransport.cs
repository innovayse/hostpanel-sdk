using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A transport that hands out one client and counts the requests for it. It implements only <c>CreateClient()</c>, as a
/// transport written before the per server client existed does.
/// </summary>
internal sealed class FixedTransport : IPanelTransport
{
    /// <summary>The client every call returns.</summary>
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of the <see cref="FixedTransport"/> class.</summary>
    /// <param name="client">The client every call returns.</param>
    internal FixedTransport(HttpClient client)
    {
        _client = client;
    }

    /// <summary>Gets how many clients were requested.</summary>
    internal int Created { get; private set; }

    /// <inheritdoc />
    public HttpClient CreateClient()
    {
        Created++;
        return _client;
    }
}
