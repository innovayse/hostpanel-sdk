using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.StoredCredentials;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Provisioning;

/// <summary>
/// Tests of <see cref="IPanelTransport.CreateClient(PanelServerConnection)"/> and of <see cref="PanelTransportContract"/>.
/// </summary>
public sealed class PanelTransportSelfSignedTests
{
    /// <summary>The server id the operator allowed a self signed certificate for.</summary>
    private const int AllowedId = 7;

    /// <summary>A transport that knows nothing about the setting answers the plain client for any server.</summary>
    [Fact]
    public void CreateClient_ForServer_DefaultsToThePlainClient()
    {
        using var client = new HttpClient();
        IPanelTransport transport = new PlainTransport(client);

        transport.CreateClient(Server(1, 443)).Should().BeSameAs(client);
        transport.CreateClient().Should().BeSameAs(client);
    }

    /// <summary>A transport that trusts one server on request passes both checks.</summary>
    [Fact]
    public async Task Contract_TrustForOneServer_Passes()
    {
        await using var listener = SelfSignedListener.Start();
        var transport = new SelfSignedTransport(AllowedId);

        await PanelTransportContract.AssertSelfSignedIsRefusedByDefaultAsync(
            transport, listener, Server(1, listener.Port));
        await PanelTransportContract.AssertSelfSignedIsAcceptedForTheServerOnlyAsync(
            transport, listener, Server(AllowedId, listener.Port));
    }

    /// <summary>A transport that trusts every request is rejected by the refusal check.</summary>
    [Fact]
    public async Task Contract_TrustForEveryRequest_Throws()
    {
        await using var listener = SelfSignedListener.Start();
        var transport = new SelfSignedTransport(AllowedId) { TrustEveryRequest = true };

        var run = () => PanelTransportContract.AssertSelfSignedIsRefusedByDefaultAsync(
            transport, listener, Server(1, listener.Port));

        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain("never be on for every request");
    }

    /// <summary>A transport that never trusts the certificate is rejected by the acceptance check.</summary>
    [Fact]
    public async Task Contract_NeverTrusts_Throws()
    {
        await using var listener = SelfSignedListener.Start();
        var transport = new SelfSignedTransport();

        var run = () => PanelTransportContract.AssertSelfSignedIsAcceptedForTheServerOnlyAsync(
            transport, listener, Server(AllowedId, listener.Port));

        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain("refused the self signed certificate");
    }

    /// <summary>The checks refuse a missing argument when they are called.</summary>
    [Fact]
    public async Task Contract_NullArguments_Throw()
    {
        await using var listener = SelfSignedListener.Start();

        var noTransport = () => PanelTransportContract.AssertSelfSignedIsRefusedByDefaultAsync(
            null!, listener, Server(1, listener.Port));
        var noListener = () => PanelTransportContract.AssertSelfSignedIsAcceptedForTheServerOnlyAsync(
            new SelfSignedTransport(), null!, Server(1, 1));

        await noTransport.Should().ThrowAsync<ArgumentNullException>();
        await noListener.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>Builds a server on the listener's host name.</summary>
    /// <param name="id">The server id.</param>
    /// <param name="port">The port of the listener.</param>
    /// <returns>The connection.</returns>
    private static PanelServerConnection Server(int id, int port)
    {
        return new PanelServerConnection(id, "local", SelfSignedListener.HostName, null, port, true, null, null, null);
    }

    /// <summary>A transport written before the per server client existed: it implements only <c>CreateClient()</c>.</summary>
    private sealed class PlainTransport : IPanelTransport
    {
        /// <summary>The client every call returns.</summary>
        private readonly HttpClient _client;

        /// <summary>Initializes a new instance of the <see cref="PlainTransport"/> class.</summary>
        /// <param name="client">The client every call returns.</param>
        public PlainTransport(HttpClient client)
        {
            _client = client;
        }

        /// <inheritdoc />
        public HttpClient CreateClient()
        {
            return _client;
        }
    }
}
