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

    /// <summary>A create that is refused, answers no reference, creates twice or changes the reference is reported with the create wording.</summary>
    [Fact]
    public async Task CreateLoop_Messages_AreUnchanged()
    {
        var refused = new ScriptedPanelPlugin { Create = _ => PanelCreateResult.Refused(PanelRefusalReason.PackageUnknown) };
        var noReference = new ScriptedPanelPlugin { Create = _ => new PanelCreateResult(PanelOperationOutcome.Succeeded, null, null, null, null) };
        var twice = new ScriptedPanelPlugin { Create = _ => PanelCreateResult.Created("acct1") };
        var changes = new ScriptedPanelPlugin
        {
            Create = call => call == 1 ? PanelCreateResult.Created("acct1") : PanelCreateResult.AlreadyExists("acct2", null),
        };

        var refusedRun = () => ProvisioningContract.AssertCreateIsIdempotentAsync(refused, PanelFixtures.Create);
        var noReferenceRun = () => ProvisioningContract.AssertCreateIsIdempotentAsync(noReference, PanelFixtures.Create);
        var twiceRun = () => ProvisioningContract.AssertCreateIsIdempotentAsync(twice, PanelFixtures.Create);
        var changesRun = () => ProvisioningContract.AssertCreateIsIdempotentAsync(changes, PanelFixtures.Create);

        (await refusedRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "CreateAccountAsync answered Refused on call 1. The caller must arrange a create the panel accepts, and a repeat must answer AlreadyApplied.");
        (await noReferenceRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "CreateAccountAsync answered Succeeded without a ProvisioningRef on call 1.");
        (await twiceRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "CreateAccountAsync answered Succeeded on call 2: the account was created again. A repeat with the same user name must answer AlreadyApplied.");
        (await changesRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "CreateAccountAsync answered a different ProvisioningRef on call 2 than on call 1. The reference must be stable across retries.");
    }

    /// <summary>The usage and the single sign on of an unknown account are reported with the account wording.</summary>
    [Fact]
    public async Task UnknownAccount_UsageAndSso_Messages_AreUnchanged()
    {
        var usage = new ScriptedPanelPlugin
        {
            Suspend = _ => PanelActionResult.NotFound(),
            Unsuspend = _ => PanelActionResult.NotFound(),
            ChangePackage = _ => PanelActionResult.NotFound(),
        };
        var sso = new ScriptedPanelPlugin
        {
            Suspend = _ => PanelActionResult.NotFound(),
            Unsuspend = _ => PanelActionResult.NotFound(),
            ChangePackage = _ => PanelActionResult.NotFound(),
            Usage = _ => PanelUsageResult.NotFound(),
        };

        var usageRun = () => ProvisioningContract.AssertUnknownAccountIsNotFoundAsync(usage, PanelFixtures.Account, new PanelPackage("basic"));
        var ssoRun = () => ProvisioningContract.AssertUnknownAccountIsNotFoundAsync(sso, PanelFixtures.Account, new PanelPackage("basic"));

        (await usageRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "GetUsageAsync answered Succeeded for an unknown account; it must answer NotFound or NotSupported.");
        (await ssoRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "CreateSsoLinkAsync answered Succeeded for an unknown account; it must answer NotFound or NotSupported.");
    }

    /// <summary>Every message of the account listing is reported with the panel wording and the page number.</summary>
    [Fact]
    public async Task List_AllMessages_AreUnchanged()
    {
        var noAccounts = new ScriptedPanelPlugin { List = _ => new PanelAccountListResult(PanelOperationOutcome.Succeeded, null!, null, null) };
        var blank = new ScriptedPanelPlugin { List = _ => PanelAccountListResult.Page([new PanelAccount(" ", null, PanelAccountStatus.Active, null, null)], null) };
        var status = new ScriptedPanelPlugin { List = _ => PanelAccountListResult.Page([new PanelAccount("a", null, default, null, null)], null) };
        var duplicate = new ScriptedPanelPlugin
        {
            List = cursor => PanelAccountListResult.Page([new PanelAccount("a", null, PanelAccountStatus.Active, null, null)], cursor is null ? "1" : null),
        };
        var stuck = new ScriptedPanelPlugin
        {
            List = cursor => PanelAccountListResult.Page([new PanelAccount("x" + cursor, null, PanelAccountStatus.Active, null, null)], cursor ?? "same"),
        };
        var endless = new ScriptedPanelPlugin
        {
            List = cursor => PanelAccountListResult.Page([new PanelAccount("a" + cursor, null, PanelAccountStatus.Active, null, null)], (cursor ?? string.Empty) + "n"),
        };

        var noAccountsRun = () => ProvisioningContract.AssertListAccountsIsCompleteAsync(noAccounts, PanelFixtures.Server);
        var blankRun = () => ProvisioningContract.AssertListAccountsIsCompleteAsync(blank, PanelFixtures.Server);
        var statusRun = () => ProvisioningContract.AssertListAccountsIsCompleteAsync(status, PanelFixtures.Server);
        var duplicateRun = () => ProvisioningContract.AssertListAccountsIsCompleteAsync(duplicate, PanelFixtures.Server);
        var stuckRun = () => ProvisioningContract.AssertListAccountsIsCompleteAsync(stuck, PanelFixtures.Server);
        var endlessRun = () => ProvisioningContract.AssertListAccountsIsCompleteAsync(endless, PanelFixtures.Server, maxPages: 3);

        (await noAccountsRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "ListAccountsAsync answered null Accounts on page 1; an empty page is an empty list.");
        (await blankRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "ListAccountsAsync returned an account without a ProvisioningRef on page 1.");
        (await statusRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "ListAccountsAsync returned an account with a Status that is not a PanelAccountStatus on page 1.");
        (await duplicateRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "ListAccountsAsync returned an account twice (page 2). Following the cursors must return every account exactly once.");
        (await stuckRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "ListAccountsAsync answered the cursor it was asked for on page 2; the listing would never end.");
        (await endlessRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "ListAccountsAsync was still returning cursors after 3 pages.");
    }

    /// <summary>A negative account count and an undefined connection outcome are reported with the connection wording.</summary>
    [Fact]
    public async Task TestConnection_OtherMessages_AreUnchanged()
    {
        var negative = new ScriptedPanelPlugin { Test = _ => new PanelConnectionTestResult(PanelConnectionOutcome.Connected, null, -1) };
        var undefined = new ScriptedPanelPlugin { Test = _ => new PanelConnectionTestResult((PanelConnectionOutcome)99, null, null) };

        var negativeRun = () => ProvisioningContract.AssertTestConnectionAnswersAsync(negative, new PanelTestConnectionContext(PanelFixtures.Server));
        var undefinedRun = () => ProvisioningContract.AssertTestConnectionAnswersAsync(undefined, new PanelTestConnectionContext(PanelFixtures.Server));

        (await negativeRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be("TestConnectionAsync answered a negative AccountCount.");
        (await undefinedRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "TestConnectionAsync answered 99, which is not a PanelConnectionOutcome.");
    }

    /// <summary>A reason that is not defined is reported with the panel reason type.</summary>
    [Fact]
    public async Task UndefinedReason_Message_IsUnchanged()
    {
        var plugin = new ScriptedPanelPlugin { Suspend = _ => new PanelActionResult(PanelOperationOutcome.Refused, (PanelRefusalReason)99) };

        var run = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(plugin, PanelFixtures.Suspend);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Be(
            "SuspendAccountAsync answered 99 as the reason on call 1, which is not a PanelRefusalReason.");
    }

    /// <summary>A leaked secret is reported without repeating it.</summary>
    [Fact]
    public void LeakedSecret_Message_IsUnchanged()
    {
        var run = () => ProvisioningContract.AssertHidesSecrets(new LeakyResult("token-secret-002"), "token-secret-002");

        run.Should().Throw<PluginContractViolationException>().Which.Message.Should().Be(
            "The printed form of LeakyResult contains secret number 1. A result, a context and a log line must print secrets masked.");
    }
}
