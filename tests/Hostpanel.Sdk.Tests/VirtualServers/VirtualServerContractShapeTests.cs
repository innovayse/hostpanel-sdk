using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;
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

    /// <summary>Every data factory sets the outcome, the value and the reason that its name promises.</summary>
    [Fact]
    public void DataResult_Factories_SetOutcomeValueAndReason()
    {
        var instance = new VirtualServerInstance("srv-1", null, null, null);

        ValueResult<VirtualServerInstance, PanelRefusalReason>.Done(instance).Should().Be(new ValueResult<VirtualServerInstance, PanelRefusalReason>(PanelOperationOutcome.Succeeded, instance, null));
        ValueResult<VirtualServerInstance, PanelRefusalReason>.AlreadyDone(instance).Outcome.Should().Be(PanelOperationOutcome.AlreadyApplied);
        ValueResult<VirtualServerInstance, PanelRefusalReason>.NotFound().Value.Should().BeNull();
        ValueResult<VirtualServerInstance, PanelRefusalReason>.NotSupported().Outcome.Should().Be(PanelOperationOutcome.NotSupported);
        ValueResult<VirtualServerInstance, PanelRefusalReason>.Refused(PanelRefusalReason.NameInvalid).Reason.Should().Be(PanelRefusalReason.NameInvalid);
    }

    /// <summary>A success cannot be built without its value.</summary>
    [Fact]
    public void DataResult_SuccessWithoutValue_Throws()
    {
        var done = () => ValueResult<VirtualServerInstance, PanelRefusalReason>.Done(null!);
        var already = () => ValueResult<VirtualServerInstance, PanelRefusalReason>.AlreadyDone(null!);

        done.Should().Throw<ArgumentNullException>();
        already.Should().Throw<ArgumentNullException>();
    }

    /// <summary>The new enum starts at 1, and the two refusal reasons added for virtual servers extend the old values without moving them.</summary>
    [Fact]
    public void Enums_AreAppendedAndStartAtOne()
    {
        Enum.IsDefined((VirtualServerPowerState)0).Should().BeFalse();
        ((int)VirtualServerPowerState.Running).Should().Be(1);
        ((int)VirtualServerPowerState.Provisioning).Should().Be(4);
        ((int)PanelRefusalReason.PackageUnknown).Should().Be(1);
        ((int)PanelRefusalReason.Other).Should().Be(7);
        ((int)PanelRefusalReason.ImageUnknown).Should().Be(8);
        ((int)PanelRefusalReason.NameInvalid).Should().Be(9);
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
