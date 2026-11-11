using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Plugins.VirtualServers;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.VirtualServers;

/// <summary>Tests of the shapes of the virtual server contract: masking, factories, enum values and the additive guarantee.</summary>
public sealed class VirtualServerContractShapeTests
{
    /// <summary>The create and the rebuild print the root password masked, and the platform credentials inside them masked too.</summary>
    [Fact]
    public void PasswordContexts_ToString_MaskPasswordAndServerSecrets()
    {
        var create = VirtualServerFixtures.Create.ToString();
        var rebuild = VirtualServerFixtures.Rebuild.ToString();

        create.Should().NotContain(VirtualServerFixtures.RootPassword).And.NotContain(PanelFixtures.AccessHash).And.Contain("RootPassword = ***").And.Contain("Name = vps-42");
        rebuild.Should().NotContain(VirtualServerFixtures.RebuildPassword).And.NotContain(PanelFixtures.ApiToken).And.Contain("RootPassword = ***");
    }

    /// <summary>Equality still sees the password, so two creates that differ only in it are not equal.</summary>
    [Fact]
    public void CreateContext_Equality_StillComparesPassword()
    {
        var other = VirtualServerFixtures.Create with { RootPassword = "different" };

        other.Should().NotBe(VirtualServerFixtures.Create);
    }

    /// <summary>A generated password and a console address print masked, an absent password prints as null.</summary>
    [Fact]
    public void ResultValues_ToString_MaskSecrets()
    {
        var instance = new VirtualServerInstance("srv-1", "192.0.2.30", null, VirtualServerFixtures.GeneratedPassword);
        var password = new VirtualServerRootPassword(VirtualServerFixtures.GeneratedPassword);
        var console = new VirtualServerConsole("https://console.example.test/s?t=abc", null);

        instance.ToString().Should().NotContain(VirtualServerFixtures.GeneratedPassword).And.Contain("GeneratedPassword = ***").And.Contain("ProvisioningRef = srv-1");
        password.ToString().Should().NotContain(VirtualServerFixtures.GeneratedPassword);
        ValueResult<VirtualServerRootPassword, PanelRefusalReason>.Done(password).ToString().Should().NotContain(VirtualServerFixtures.GeneratedPassword);
        console.ToString().Should().NotContain("t=abc").And.Contain("Url = ***");
        new VirtualServerRootPassword(null).ToString().Should().Contain("GeneratedPassword = null");
        new VirtualServerInstance("srv-1", null, null, null).ToString().Should().Contain("GeneratedPassword = null");
    }

    /// <summary>The capability name the manifest lists is fixed, and the plugin type stays provisioning, so no type value is added.</summary>
    [Fact]
    public void Capability_Name_IsVirtualServer_AndPluginTypeIsUnchanged()
    {
        PluginCapabilities.VirtualServer.Should().Be("virtualServer");
        Enum.GetValues<PluginType>().Should().HaveCount(7);
    }

    /// <summary>The server listing is a separate opt in that extends the virtual server interface and has its own capability name.</summary>
    [Fact]
    public void ServerListing_IsASeparateOptIn()
    {
        typeof(IVirtualServerListPlugin).GetInterfaces().Should().Contain(typeof(IVirtualServerPlugin));
        typeof(IVirtualServerPlugin).GetMethods().Select(method => method.Name).Should().NotContain("ListServersAsync");
        PluginCapabilities.VirtualServerList.Should().Be("virtualServerList");
    }

    /// <summary>
    /// A provisioning plugin built on an earlier version, with or without panel operations, is not a virtual server plugin;
    /// the new interface is a separate opt in on the same marker, and the marker gains no member.
    /// </summary>
    [Fact]
    public void ExistingProvisioningPlugins_AreNotVirtualServerPlugins()
    {
        IProvisioningPlugin panel = new ScriptedPanelPlugin();
        IProvisioningPlugin server = new ScriptedVirtualServerPlugin();

        panel.Should().NotBeAssignableTo<IVirtualServerPlugin>();
        server.Should().BeAssignableTo<IVirtualServerPlugin>().And.NotBeAssignableTo<IProvisioningPanelPlugin>();
        typeof(IProvisioningPlugin).GetMembers().Should().BeEmpty();
        typeof(IVirtualServerPlugin).GetInterfaces().Should().Contain(typeof(IProvisioningPlugin));
    }
}
