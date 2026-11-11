using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.StoredCredentials;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Provisioning;

/// <summary>
/// Pins the exact text of the violation messages of <see cref="ProvisioningContract"/>. The checks behind them are shared
/// with the registrar and virtual server contracts, and these tests keep the shared code from changing what a provisioning
/// plugin author already reads.
/// </summary>
public sealed class ProvisioningContractMessageTests
{
    /// <summary>A call that throws is reported with the exception type, the call number and the panel wording.</summary>
    [Fact]
    public async Task Throwing_Message_IsUnchanged()
    {
        var plugin = new ScriptedPanelPlugin { Suspend = _ => throw new InvalidOperationException("panel text") };

        var run = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(plugin, PanelFixtures.Suspend);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "SuspendAccountAsync threw on call 1: InvalidOperationException. A refusal, a missing account and an unsupported operation are outcomes, and a repeat must answer the same as the first call.");
    }

    /// <summary>A call that answers an outcome that does not fit is reported with the account and panel wording.</summary>
    [Fact]
    public async Task WrongOutcome_Message_IsUnchanged()
    {
        var plugin = new ScriptedPanelPlugin { Suspend = _ => PanelActionResult.NotFound() };

        var run = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(plugin, PanelFixtures.Suspend);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "SuspendAccountAsync answered NotFound on call 1. The caller must arrange an account the panel accepts the operation for, and a repeat must answer Succeeded or AlreadyApplied.");
    }

    /// <summary>A null answer is reported with the call number.</summary>
    [Fact]
    public async Task NullAnswer_Message_IsUnchanged()
    {
        var plugin = new ScriptedPanelPlugin { Suspend = _ => null! };

        var run = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(plugin, PanelFixtures.Suspend);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be("SuspendAccountAsync answered null on call 1.");
    }

    /// <summary>A refusal without a reason, and a reason on a success, are reported with the panel reason type.</summary>
    [Fact]
    public async Task Shape_Messages_AreUnchanged()
    {
        var noReason = new ScriptedPanelPlugin { Suspend = _ => new PanelActionResult(PanelOperationOutcome.Refused, null) };
        var strayReason = new ScriptedPanelPlugin { Suspend = _ => new PanelActionResult(PanelOperationOutcome.Succeeded, PanelRefusalReason.Other) };
        var undefined = new ScriptedPanelPlugin { Suspend = _ => new PanelActionResult((PanelOperationOutcome)99, null) };

        var missing = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(noReason, PanelFixtures.Suspend);
        var stray = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(strayReason, PanelFixtures.Suspend);
        var notDefined = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(undefined, PanelFixtures.Suspend);

        (await missing.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be("SuspendAccountAsync refused on call 1 without a PanelRefusalReason.");
        (await stray.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "SuspendAccountAsync answered Succeeded with a PanelRefusalReason on call 1; only a refusal has one.");
        (await notDefined.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "SuspendAccountAsync answered 99 on call 1, which is not a PanelOperationOutcome.");
    }

    /// <summary>An outcome the unknown account promise does not allow is reported with the account wording.</summary>
    [Fact]
    public async Task UnknownAccount_Message_IsUnchanged()
    {
        var plugin = new ScriptedPanelPlugin { Suspend = _ => PanelActionResult.Done() };

        var run = () => ProvisioningContract.AssertUnknownAccountIsNotFoundAsync(plugin, PanelFixtures.Account, new PanelPackage("basic"));

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "SuspendAccountAsync answered Succeeded for an unknown account; it must answer NotFound.");
    }

    /// <summary>A listing that is not supported is reported with the panel wording and the page number.</summary>
    [Fact]
    public async Task List_Message_IsUnchanged()
    {
        var plugin = new ScriptedPanelPlugin { List = _ => PanelAccountListResult.NotSupported() };

        var run = () => ProvisioningContract.AssertListAccountsIsCompleteAsync(plugin, PanelFixtures.Server);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "ListAccountsAsync answered NotSupported on page 1; the caller must arrange a panel that lists.");
    }

    /// <summary>A connection test with a count after a failure is reported with the panel connection wording.</summary>
    [Fact]
    public async Task TestConnection_Message_IsUnchanged()
    {
        var plugin = new ScriptedPanelPlugin { Test = _ => new PanelConnectionTestResult(PanelConnectionOutcome.Unreachable, "1.0", null) };

        var run = () => ProvisioningContract.AssertTestConnectionAnswersAsync(plugin, new PanelTestConnectionContext(PanelFixtures.Server));

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "TestConnectionAsync answered Unreachable with a Version or an AccountCount; only a connected test has them.");
    }
}
