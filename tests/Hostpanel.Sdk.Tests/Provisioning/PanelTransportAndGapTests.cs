using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Provisioning;

/// <summary>Tests of the transport port, the connection and result members added for URLs and TLS, and the retired panel area.</summary>
public sealed class PanelTransportAndGapTests
{
    /// <summary>A panel plugin can take the transport in its constructor and build its requests on the client it creates.</summary>
    [Fact]
    public void Transport_TakenInConstructor_CreatesClient()
    {
        using var client = new HttpClient();
        var transport = new FixedTransport(client);

        var plugin = new TransportUsingPlugin(transport);

        plugin.Client.Should().BeSameAs(client);
        transport.Created.Should().Be(1);
    }

    /// <summary>The connection prints whether TLS is used, and still masks the secrets.</summary>
    [Fact]
    public void ServerConnection_UseSsl_IsPrintedAndSecretsStayMasked()
    {
        var printed = PanelFixtures.Server.ToString();

        printed.Should().Contain("UseSsl = True").And.NotContain(PanelFixtures.ApiToken);
        (PanelFixtures.Server with { UseSsl = false }).Should().NotBe(PanelFixtures.Server);
    }

    /// <summary>A null port is a legal value and means the plugin's own default.</summary>
    [Fact]
    public void ServerConnection_NullPort_IsAllowed()
    {
        var server = PanelFixtures.Server with { Port = null };

        server.Port.Should().BeNull();
        server.ToString().Should().Contain("Port = ,");
    }

    /// <summary>The create result and the listed account carry the control panel address when the panel tells it.</summary>
    [Fact]
    public void ControlPanelUrl_IsCarriedByCreateResultAndAccount()
    {
        PanelCreateResult.Created("a", "https://panel.example.test:2083").ControlPanelUrl.Should().Be("https://panel.example.test:2083");
        PanelCreateResult.AlreadyExists("a", "d.test", "https://panel.example.test:2083").ControlPanelUrl.Should().NotBeNull();
        PanelCreateResult.Created("a").ControlPanelUrl.Should().BeNull();
        PanelCreateResult.Refused(PanelRefusalReason.Other).ControlPanelUrl.Should().BeNull();
        new PanelAccount("a", null, PanelAccountStatus.Active, null, "https://panel.example.test:2083").ControlPanelUrl.Should().NotBeNull();
    }

    /// <summary>The create context requires the client; its reference is always set, its e-mail address and name may be absent.</summary>
    [Fact]
    public void CreateContext_CarriesClientReferenceEmailAndName()
    {
        var client = PanelFixtures.Create.Client;

        client.ClientReference.Should().NotBeNullOrEmpty();
        client.Email.Should().NotBeNull();
        client.Name.Should().NotBeNull();
    }

    /// <summary>Every type of the retired panel area is marked obsolete, points to the replacement and names the version that removes it.</summary>
    [Fact]
    public void HostingPanelArea_IsObsolete_AndPointsToThePanelPlugin()
    {
        var types = typeof(IProvisioningPanelPlugin).Assembly.GetTypes()
            .Where(type => type.Namespace == "Hostpanel.Sdk.Plugins.HostingPanel" && !type.Name.StartsWith('<'))
            .ToList();

        types.Should().NotBeEmpty();
        foreach (var type in types.Where(type => type.DeclaringType is null))
        {
            var attribute = (ObsoleteAttribute?)Attribute.GetCustomAttribute(type, typeof(ObsoleteAttribute));
            attribute.Should().NotBeNull(type.Name);
            attribute!.Message.Should().Contain(nameof(IProvisioningPanelPlugin)).And.Contain("4.0.0", "the message names the version that removes the type");
        }
    }

    /// <summary>The contract helper is public, so a plugin repository can call it from its own tests.</summary>
    [Fact]
    public void ProvisioningContract_IsPublic()
    {
        typeof(ProvisioningContract).IsPublic.Should().BeTrue();
        typeof(ProvisioningContract).IsAbstract.Should().BeTrue("a static class is abstract and sealed");
    }

    /// <summary>A plugin shape that takes the transport in its constructor, as the contract requires.</summary>
    private sealed class TransportUsingPlugin
    {
        /// <summary>Initializes a new instance of the <see cref="TransportUsingPlugin"/> class.</summary>
        /// <param name="transport">The host's transport.</param>
        public TransportUsingPlugin(IPanelTransport transport)
        {
            Client = transport.CreateClient();
        }

        /// <summary>Gets the client the plugin builds its requests on.</summary>
        public HttpClient Client { get; }
    }
}
