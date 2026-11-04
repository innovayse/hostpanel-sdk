using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Provisioning;

/// <summary>Tests of the shapes of the provisioning panel contract: masking, factories, enum values and the additive guarantee.</summary>
public sealed class PanelContractShapeTests
{
    /// <summary>The server connection prints its two secrets masked and everything else as is.</summary>
    [Fact]
    public void ServerConnection_ToString_MasksSecrets()
    {
        var printed = PanelFixtures.Server.ToString();

        printed.Should().NotContain(PanelFixtures.AccessHash).And.NotContain(PanelFixtures.ApiToken);
        printed.Should().Contain("AccessHash = ***").And.Contain("ApiToken = ***").And.Contain("panel.example.test").And.Contain("Username = root");
    }

    /// <summary>An absent secret prints as null, so a log line still shows that none was saved.</summary>
    [Fact]
    public void ServerConnection_ToString_ShowsAbsentSecretAsNull()
    {
        var server = PanelFixtures.Server with { AccessHash = null, ApiToken = null };

        server.ToString().Should().Contain("AccessHash = null").And.Contain("ApiToken = null");
    }

    /// <summary>The create and the password change print the password masked, and the server inside them masked too.</summary>
    [Fact]
    public void PasswordContexts_ToString_MaskPasswordAndServerSecrets()
    {
        var create = PanelFixtures.Create.ToString();
        var change = PanelFixtures.ChangePassword.ToString();

        create.Should().NotContain(PanelFixtures.Password).And.NotContain(PanelFixtures.AccessHash).And.Contain("Password = ***");
        change.Should().NotContain(PanelFixtures.NewPassword).And.NotContain(PanelFixtures.ApiToken).And.Contain("NewPassword = ***");
    }

    /// <summary>Equality still sees the secrets, so two contexts that differ only in a password are not equal.</summary>
    [Fact]
    public void PasswordContexts_Equality_StillComparesSecrets()
    {
        var other = PanelFixtures.Create with { Password = "different" };

        other.Should().NotBe(PanelFixtures.Create);
    }

    /// <summary>The single sign on link is a credential and prints masked.</summary>
    [Fact]
    public void SsoResult_ToString_MasksUrl()
    {
        var result = PanelSsoResult.Issued("https://panel.example.test/login?t=abc");

        result.ToString().Should().NotContain("login?t=abc").And.Contain("Url = ***").And.Contain("Succeeded");
        PanelSsoResult.NotFound().ToString().Should().Contain("Url = null");
    }

    /// <summary>Every factory sets the outcome, the reference and the reason that its name promises.</summary>
    [Fact]
    public void Factories_SetOutcomeAndReason()
    {
        PanelCreateResult.Created("a").Should().Be(new PanelCreateResult(PanelOperationOutcome.Succeeded, "a", null, null, null));
        PanelCreateResult.AlreadyExists("a", "d.test").Should().Be(new PanelCreateResult(PanelOperationOutcome.AlreadyApplied, "a", "d.test", null, null));
        PanelCreateResult.Refused(PanelRefusalReason.UsernameInvalid).Should().Be(new PanelCreateResult(PanelOperationOutcome.Refused, null, null, null, PanelRefusalReason.UsernameInvalid));
        PanelActionResult.Done().Outcome.Should().Be(PanelOperationOutcome.Succeeded);
        PanelActionResult.AlreadyDone().Outcome.Should().Be(PanelOperationOutcome.AlreadyApplied);
        PanelActionResult.NotFound().Outcome.Should().Be(PanelOperationOutcome.NotFound);
        PanelActionResult.NotSupported().Outcome.Should().Be(PanelOperationOutcome.NotSupported);
        PanelActionResult.Refused(PanelRefusalReason.LimitReached).Should().Be(new PanelActionResult(PanelOperationOutcome.Refused, PanelRefusalReason.LimitReached));
        PanelUsageResult.Refused(PanelRefusalReason.AccessDenied).Reason.Should().Be(PanelRefusalReason.AccessDenied);
        PanelUsageResult.Read(new PanelUsage(1, null, null, null, DateTimeOffset.UnixEpoch)).Usage!.DiskUsedBytes.Should().Be(1);
        PanelAccountListResult.NotSupported().Accounts.Should().BeEmpty();
        PanelAccountListResult.Refused(PanelRefusalReason.AccessDenied).Outcome.Should().Be(PanelOperationOutcome.Refused);
        PanelAccountListResult.Page([new PanelAccount("a", null, PanelAccountStatus.Suspended, "basic", null)], "next").NextCursor.Should().Be("next");
    }

    /// <summary>A failed connection test cannot be built with the Connected outcome, and a connected one carries its figures.</summary>
    [Fact]
    public void ConnectionTestResult_Factories_RefuseConnectedFailure()
    {
        var build = () => PanelConnectionTestResult.Failed(PanelConnectionOutcome.Connected);

        PanelConnectionTestResult.Connected("2.1", 0).AccountCount.Should().Be(0);
        PanelConnectionTestResult.Failed(PanelConnectionOutcome.Unreachable).Version.Should().BeNull();
        build.Should().Throw<ArgumentException>();
    }

    /// <summary>The outcome enums start at 1, so a default-initialised value is not a valid answer.</summary>
    [Fact]
    public void Enums_StartAtOne_AndZeroIsUndefined()
    {
        Enum.IsDefined((PanelOperationOutcome)0).Should().BeFalse();
        Enum.IsDefined((PanelConnectionOutcome)0).Should().BeFalse();
        Enum.IsDefined((PanelRefusalReason)0).Should().BeFalse();
        Enum.IsDefined((PanelAccountStatus)0).Should().BeFalse();
        ((int)PanelOperationOutcome.Succeeded).Should().Be(1);
        ((int)PanelConnectionOutcome.Connected).Should().Be(1);
        ((int)PanelRefusalReason.PackageUnknown).Should().Be(1);
        ((int)PanelAccountStatus.Active).Should().Be(1);
    }

    /// <summary>The capability name the manifest lists is fixed.</summary>
    [Fact]
    public void Capability_Name_IsProvisioningPanel()
    {
        PluginCapabilities.ProvisioningPanel.Should().Be("provisioningPanel");
    }

    /// <summary>
    /// A provisioning plugin built on 3.0.0 implements only the marker and compiles unchanged; the new interface is a
    /// separate opt in, so the marker and the base class gain no member.
    /// </summary>
    [Fact]
    public void MarkerOnlyPlugin_IsNotAPanelPlugin_AndMarkerHasNoMembers()
    {
        IProvisioningPlugin legacy = new MarkerOnlyPlugin();
        IProvisioningPlugin panel = new ScriptedPanelPlugin();

        legacy.Should().NotBeAssignableTo<IProvisioningPanelPlugin>();
        panel.Should().BeAssignableTo<IProvisioningPanelPlugin>();
        typeof(IProvisioningPlugin).GetMembers().Should().BeEmpty();
        typeof(IProvisioningPanelPlugin).GetInterfaces().Should().Contain(typeof(IProvisioningPlugin));
    }

    /// <summary>A plugin written the 3.0.0 way: it implements the marker and nothing else.</summary>
    private sealed class MarkerOnlyPlugin : IProvisioningPlugin
    {
    }
}
