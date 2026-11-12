using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.StoredCredentials;
using Hostpanel.Sdk.Tests.Fakes;
using static Hostpanel.Sdk.Tests.Support.ContractViolationAsserts;

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
        IPanelTransport transport = new FixedTransport(client);

        transport.CreateClient(Server(1, 443)).Should().BeSameAs(client);
        transport.CreateClient().Should().BeSameAs(client);
    }

    /// <summary>A transport that trusts one server on request passes both checks.</summary>
    [Fact]
    public async Task Contract_TrustForOneServer_Passes()
    {
        await using var listener = SelfSignedListener.Start();
        var transport = new SelfSignedTransport(AllowedId);

        await PanelTransportContract.AssertSelfSignedIsRefusedByDefaultAsync(transport, Server(1, listener.Port));
        await PanelTransportContract.AssertSelfSignedIsAcceptedForTheServerOnlyAsync(
            transport, Server(AllowedId, listener.Port));
    }

    /// <summary>A transport that trusts every request is rejected by the refusal check.</summary>
    [Fact]
    public async Task Contract_TrustForEveryRequest_Throws()
    {
        await using var listener = SelfSignedListener.Start();
        var transport = new SelfSignedTransport(AllowedId) { TrustEveryRequest = true };

        var run = () => PanelTransportContract.AssertSelfSignedIsRefusedByDefaultAsync(
            transport, Server(1, listener.Port));

        var message = await ViolationMessageAsync(run);
        message.Should().Contain("never be on for every request");
    }

    /// <summary>A transport that never trusts the certificate is rejected by the acceptance check.</summary>
    [Fact]
    public async Task Contract_NeverTrusts_Throws()
    {
        await using var listener = SelfSignedListener.Start();
        var transport = new SelfSignedTransport();

        var run = () => PanelTransportContract.AssertSelfSignedIsAcceptedForTheServerOnlyAsync(
            transport, Server(AllowedId, listener.Port));

        var message = await ViolationMessageAsync(run);
        message.Should().Contain("refused the self signed certificate");
    }

    /// <summary>A transport that trusts the server on every port is rejected.</summary>
    [Fact]
    public async Task Contract_TrustForEveryPort_Throws()
    {
        await using var listener = SelfSignedListener.Start();
        var transport = new SelfSignedTransport(AllowedId) { TrustAnyPort = true };

        var run = () => PanelTransportContract.AssertSelfSignedIsAcceptedForTheServerOnlyAsync(
            transport, Server(AllowedId, listener.Port));

        var message = await ViolationMessageAsync(run);
        message.Should().Contain("another port");
    }

    /// <summary>A transport that trusts the server under every name is rejected.</summary>
    [Fact]
    public async Task Contract_TrustForEveryName_Throws()
    {
        await using var listener = SelfSignedListener.Start();
        var transport = new SelfSignedTransport(AllowedId) { TrustAnyName = true };

        var run = () => PanelTransportContract.AssertSelfSignedIsAcceptedForTheServerOnlyAsync(
            transport, Server(AllowedId, listener.Port));

        var message = await ViolationMessageAsync(run);
        message.Should().Contain("another host name");
    }

    /// <summary>A failure that is not a refusal of the certificate is reported as a broken check, not as trust.</summary>
    [Fact]
    public async Task Contract_ListenerNotReachable_Throws()
    {
        int closedPort;
        await using (var listener = SelfSignedListener.Start())
        {
            closedPort = listener.Port;
        }

        var run = () => PanelTransportContract.AssertSelfSignedIsRefusedByDefaultAsync(
            new SelfSignedTransport(), Server(1, closedPort));

        var message = await ViolationMessageAsync(run);
        message.Should().Contain("other than the certificate");
    }

    /// <summary>The checks refuse a missing argument when they are called.</summary>
    [Fact]
    public async Task Contract_NullArguments_Throw()
    {
        await using var listener = SelfSignedListener.Start();

        var noTransport = () => PanelTransportContract.AssertSelfSignedIsRefusedByDefaultAsync(
            null!, Server(1, listener.Port));
        var noServer = () => PanelTransportContract.AssertSelfSignedIsAcceptedForTheServerOnlyAsync(
            new SelfSignedTransport(), null!);
        var noPort = () => PanelTransportContract.AssertSelfSignedIsRefusedByDefaultAsync(
            new SelfSignedTransport(),
            new PanelServerConnection(1, "local", SelfSignedListener.HostName, null, null, true, null, null, null));

        await noTransport.Should().ThrowAsync<ArgumentNullException>();
        await noServer.Should().ThrowAsync<ArgumentNullException>();
        await noPort.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>Builds a server on the listener's host name.</summary>
    /// <param name="id">The server id.</param>
    /// <param name="port">The port of the listener.</param>
    /// <returns>The connection.</returns>
    private static PanelServerConnection Server(int id, int port)
    {
        return new PanelServerConnection(id, "local", SelfSignedListener.HostName, null, port, true, null, null, null);
    }
}
