using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.StoredCredentials;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Provisioning;

/// <summary>Tests of <see cref="ProvisioningContract"/>: it accepts a well behaved plugin and rejects each way of breaking a promise.</summary>
public sealed class ProvisioningContractTests
{
    /// <summary>A create that finds its own account on every repeat passes, and the helper calls it as often as asked.</summary>
    [Fact]
    public async Task AssertCreate_RepeatFindsTheAccount_Passes()
    {
        var plugin = new ScriptedPanelPlugin();

        var first = await ProvisioningContract.AssertCreateIsIdempotentAsync(plugin, PanelFixtures.Create, calls: 4);

        first.Should().Be(PanelCreateResult.Created("acct1"));
        plugin.CallsOf(nameof(IProvisioningPanelPlugin.CreateAccountAsync)).Should().Be(4);
        plugin.LastCreateServer.Should().Be(PanelFixtures.Server);
    }

    /// <summary>A first call that finds an account an earlier attempt created also passes.</summary>
    [Fact]
    public async Task AssertCreate_FirstCallAlreadyExists_Passes()
    {
        var plugin = new ScriptedPanelPlugin { Create = _ => PanelCreateResult.AlreadyExists("acct1", null) };

        var first = await ProvisioningContract.AssertCreateIsIdempotentAsync(plugin, PanelFixtures.Create);

        first.Outcome.Should().Be(PanelOperationOutcome.AlreadyApplied);
    }

    /// <summary>A plugin that creates the account again on a repeat is rejected, naming the call.</summary>
    [Fact]
    public async Task AssertCreate_CreatesTwice_Throws()
    {
        var plugin = new ScriptedPanelPlugin { Create = _ => PanelCreateResult.Created("acct1") };

        var run = () => ProvisioningContract.AssertCreateIsIdempotentAsync(plugin, PanelFixtures.Create);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("call 2");
    }

    /// <summary>A plugin that answers another reference on a repeat is rejected.</summary>
    [Fact]
    public async Task AssertCreate_ReferenceChanges_Throws()
    {
        var plugin = new ScriptedPanelPlugin
        {
            Create = call => call == 1 ? PanelCreateResult.Created("acct1") : PanelCreateResult.AlreadyExists("acct2", null),
        };

        var run = () => ProvisioningContract.AssertCreateIsIdempotentAsync(plugin, PanelFixtures.Create);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("ProvisioningRef");
    }

    /// <summary>A plugin that throws on the repeat is rejected, with the exception type and not its text.</summary>
    [Fact]
    public async Task AssertCreate_ThrowsOnRepeat_ThrowsWithTypeOnly()
    {
        var plugin = new ScriptedPanelPlugin
        {
            Create = call => call == 1 ? PanelCreateResult.Created("acct1") : throw new InvalidOperationException("user acct1 exists on panel.example.test"),
        };

        var run = () => ProvisioningContract.AssertCreateIsIdempotentAsync(plugin, PanelFixtures.Create);

        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain("InvalidOperationException").And.NotContain("panel.example.test");
    }

    /// <summary>A refusal while the caller arranged a create the panel accepts is reported, not passed.</summary>
    [Fact]
    public async Task AssertCreate_Refused_Throws()
    {
        var plugin = new ScriptedPanelPlugin { Create = _ => PanelCreateResult.Refused(PanelRefusalReason.PackageUnknown) };

        var run = () => ProvisioningContract.AssertCreateIsIdempotentAsync(plugin, PanelFixtures.Create);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("Refused");
    }

    /// <summary>A success without a reference is rejected, because the host could not address the account afterwards.</summary>
    [Fact]
    public async Task AssertCreate_NoReference_Throws()
    {
        var plugin = new ScriptedPanelPlugin { Create = _ => new PanelCreateResult(PanelOperationOutcome.Succeeded, null, null, null, null) };

        var run = () => ProvisioningContract.AssertCreateIsIdempotentAsync(plugin, PanelFixtures.Create);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("ProvisioningRef");
    }

    /// <summary>A refusal without a reason, and a reason on a success, are rejected: the outcome and the reason must agree.</summary>
    [Fact]
    public async Task AssertSuspend_ReasonAndOutcomeDisagree_Throws()
    {
        var withoutReason = new ScriptedPanelPlugin { Suspend = _ => new PanelActionResult(PanelOperationOutcome.Refused, null) };
        var withReason = new ScriptedPanelPlugin { Suspend = _ => new PanelActionResult(PanelOperationOutcome.Succeeded, PanelRefusalReason.Other) };

        var first = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(withoutReason, PanelFixtures.Suspend);
        var second = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(withReason, PanelFixtures.Suspend);

        await first.Should().ThrowAsync<PluginContractViolationException>();
        await second.Should().ThrowAsync<PluginContractViolationException>();
    }

    /// <summary>A value outside the outcome enum, such as a default-initialised zero, is rejected.</summary>
    [Fact]
    public async Task AssertSuspend_UndefinedOutcome_Throws()
    {
        var plugin = new ScriptedPanelPlugin { Suspend = _ => new PanelActionResult(default, null) };

        var run = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(plugin, PanelFixtures.Suspend);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("not a PanelOperationOutcome");
    }

    /// <summary>Suspend, unsuspend, change package and change password pass when every call succeeds or is already applied.</summary>
    [Fact]
    public async Task AssertActions_SucceedOrAlreadyApplied_Pass()
    {
        var plugin = new ScriptedPanelPlugin();

        var suspend = await ProvisioningContract.AssertSuspendIsIdempotentAsync(plugin, PanelFixtures.Suspend);
        var unsuspend = await ProvisioningContract.AssertUnsuspendIsIdempotentAsync(plugin, PanelFixtures.Account);
        var package = await ProvisioningContract.AssertChangePackageIsIdempotentAsync(plugin, PanelFixtures.ChangePackage, calls: 2);
        var password = await ProvisioningContract.AssertChangePasswordIsIdempotentAsync(plugin, PanelFixtures.ChangePassword);

        new[] { suspend, unsuspend, package, password }.Should().OnlyContain(result => result.Outcome == PanelOperationOutcome.Succeeded);
        plugin.CallsOf(nameof(IProvisioningPanelPlugin.ChangePackageAsync)).Should().Be(2);
    }

    /// <summary>A repeat that answers NotFound after the first success is rejected.</summary>
    [Fact]
    public async Task AssertSuspend_NotFoundOnRepeat_Throws()
    {
        var plugin = new ScriptedPanelPlugin { Suspend = call => call == 1 ? PanelActionResult.Done() : PanelActionResult.NotFound() };

        var run = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(plugin, PanelFixtures.Suspend);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("call 2").And.Contain("NotFound");
    }

    /// <summary>A terminate whose repeat answers NotFound is rejected: an account that is gone is already terminated.</summary>
    [Fact]
    public async Task AssertTerminate_NotFoundOnRepeat_Throws()
    {
        var plugin = new ScriptedPanelPlugin { Terminate = call => call == 1 ? PanelActionResult.Done() : PanelActionResult.NotFound() };

        var run = () => ProvisioningContract.AssertTerminateIsIdempotentAsync(plugin, PanelFixtures.Terminate);

        await run.Should().ThrowAsync<PluginContractViolationException>();
    }

    /// <summary>A terminate that answers AlreadyApplied for a gone account passes.</summary>
    [Fact]
    public async Task AssertTerminate_AlreadyGone_Passes()
    {
        var plugin = new ScriptedPanelPlugin { Terminate = _ => PanelActionResult.AlreadyDone() };

        var first = await ProvisioningContract.AssertTerminateIsIdempotentAsync(plugin, PanelFixtures.Terminate);

        first.Outcome.Should().Be(PanelOperationOutcome.AlreadyApplied);
    }

    /// <summary>A terminate that throws is rejected: the caller arranged a panel that answers.</summary>
    [Fact]
    public async Task AssertTerminate_Throws_Throws()
    {
        var plugin = new ScriptedPanelPlugin { Terminate = _ => throw new HttpRequestException("boom") };

        var run = () => ProvisioningContract.AssertTerminateIsIdempotentAsync(plugin, PanelFixtures.Terminate);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("HttpRequestException");
    }

    /// <summary>A call count below two, a missing plugin and a missing context are mistakes in the test and are refused.</summary>
    [Fact]
    public async Task Assert_BadArguments_AreRefused()
    {
        var plugin = new ScriptedPanelPlugin();

        var fewCalls = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(plugin, PanelFixtures.Suspend, calls: 1);
        var noPlugin = () => ProvisioningContract.AssertCreateIsIdempotentAsync(null!, PanelFixtures.Create);
        var noContext = () => ProvisioningContract.AssertCreateIsIdempotentAsync(plugin, null!);

        await fewCalls.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await noPlugin.Should().ThrowAsync<ArgumentNullException>();
        await noContext.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>A plugin that answers NotFound, NotSupported and an already-gone terminate for an unknown account passes.</summary>
    [Fact]
    public async Task AssertUnknownAccount_WellBehaved_Passes()
    {
        var plugin = new ScriptedPanelPlugin
        {
            Suspend = _ => PanelActionResult.NotFound(),
            Unsuspend = _ => PanelActionResult.NotFound(),
            ChangePackage = _ => PanelActionResult.NotFound(),
            Terminate = _ => PanelActionResult.AlreadyDone(),
            Usage = _ => PanelUsageResult.NotSupported(),
            Sso = _ => PanelSsoResult.NotFound(),
        };

        await ProvisioningContract.AssertUnknownAccountIsNotFoundAsync(plugin, PanelFixtures.Account, new PanelPackage("pro"));

        plugin.CallsOf(nameof(IProvisioningPanelPlugin.TerminateAccountAsync)).Should().Be(1);
    }

    /// <summary>A plugin that refuses instead of answering NotFound for an unknown account is rejected.</summary>
    [Fact]
    public async Task AssertUnknownAccount_RefusesInsteadOfNotFound_Throws()
    {
        var plugin = new ScriptedPanelPlugin
        {
            Suspend = _ => PanelActionResult.Refused(PanelRefusalReason.Other),
            Usage = _ => PanelUsageResult.NotFound(),
            Sso = _ => PanelSsoResult.NotFound(),
        };

        var run = () => ProvisioningContract.AssertUnknownAccountIsNotFoundAsync(plugin, PanelFixtures.Account, new PanelPackage("pro"));

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("SuspendAccountAsync");
    }

    /// <summary>A plugin that issues a link for an unknown account is rejected.</summary>
    [Fact]
    public async Task AssertUnknownAccount_IssuesLink_Throws()
    {
        var plugin = new ScriptedPanelPlugin
        {
            Suspend = _ => PanelActionResult.NotFound(),
            Unsuspend = _ => PanelActionResult.NotFound(),
            ChangePackage = _ => PanelActionResult.NotFound(),
            Terminate = _ => PanelActionResult.AlreadyDone(),
            Usage = _ => PanelUsageResult.NotFound(),
        };

        var run = () => ProvisioningContract.AssertUnknownAccountIsNotFoundAsync(plugin, PanelFixtures.Account, new PanelPackage("pro"));

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("CreateSsoLinkAsync");
    }

    /// <summary>A connected test, and each failure outcome without a version or a count, pass.</summary>
    [Theory]
    [InlineData(PanelConnectionOutcome.NotConfigured)]
    [InlineData(PanelConnectionOutcome.AuthenticationFailed)]
    [InlineData(PanelConnectionOutcome.Unreachable)]
    public async Task AssertTestConnection_FailureOutcomes_Pass(PanelConnectionOutcome outcome)
    {
        var plugin = new ScriptedPanelPlugin { Test = _ => PanelConnectionTestResult.Failed(outcome) };

        var result = await ProvisioningContract.AssertTestConnectionAnswersAsync(plugin, new PanelTestConnectionContext(PanelFixtures.Server));

        result.Outcome.Should().Be(outcome);
    }

    /// <summary>A connected test returns its version and count.</summary>
    [Fact]
    public async Task AssertTestConnection_Connected_ReturnsResult()
    {
        var plugin = new ScriptedPanelPlugin();

        var result = await ProvisioningContract.AssertTestConnectionAnswersAsync(plugin, new PanelTestConnectionContext(PanelFixtures.Server));

        result.Should().Be(PanelConnectionTestResult.Connected("1.0", 3));
    }

    /// <summary>A test that throws for an unreachable panel is rejected: finding that out is its purpose.</summary>
    [Fact]
    public async Task AssertTestConnection_Throws_Throws()
    {
        var plugin = new ScriptedPanelPlugin { Test = _ => throw new TimeoutException() };

        var run = () => ProvisioningContract.AssertTestConnectionAnswersAsync(plugin, new PanelTestConnectionContext(PanelFixtures.Server));

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("TimeoutException");
    }

    /// <summary>A failed test that still carries a version, a negative count or an undefined outcome is rejected.</summary>
    [Fact]
    public async Task AssertTestConnection_InconsistentResult_Throws()
    {
        var withVersion = new ScriptedPanelPlugin { Test = _ => new PanelConnectionTestResult(PanelConnectionOutcome.Unreachable, "1.0", null) };
        var negative = new ScriptedPanelPlugin { Test = _ => new PanelConnectionTestResult(PanelConnectionOutcome.Connected, null, -1) };
        var undefined = new ScriptedPanelPlugin { Test = _ => new PanelConnectionTestResult(default, null, null) };
        var context = new PanelTestConnectionContext(PanelFixtures.Server);

        await ((Func<Task>)(() => ProvisioningContract.AssertTestConnectionAnswersAsync(withVersion, context))).Should().ThrowAsync<PluginContractViolationException>();
        await ((Func<Task>)(() => ProvisioningContract.AssertTestConnectionAnswersAsync(negative, context))).Should().ThrowAsync<PluginContractViolationException>();
        await ((Func<Task>)(() => ProvisioningContract.AssertTestConnectionAnswersAsync(undefined, context))).Should().ThrowAsync<PluginContractViolationException>();
    }

    /// <summary>A listing in three pages returns every account once, in order.</summary>
    [Fact]
    public async Task AssertList_Pages_ReturnsEveryAccountOnce()
    {
        var plugin = new ScriptedPanelPlugin { List = PagesOf("a", "b", "c") };

        var accounts = await ProvisioningContract.AssertListAccountsIsCompleteAsync(plugin, PanelFixtures.Server);

        accounts.Select(account => account.ProvisioningRef).Should().Equal("a", "b", "c");
        plugin.CallsOf(nameof(IProvisioningPanelPlugin.ListAccountsAsync)).Should().Be(3);
    }

    /// <summary>An account that appears on two pages is rejected.</summary>
    [Fact]
    public async Task AssertList_DuplicateAccount_Throws()
    {
        var plugin = new ScriptedPanelPlugin { List = PagesOf("a", "b", "a") };

        var run = () => ProvisioningContract.AssertListAccountsIsCompleteAsync(plugin, PanelFixtures.Server);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("twice");
    }

    /// <summary>A page that returns the cursor it was asked for is rejected instead of looping.</summary>
    [Fact]
    public async Task AssertList_CursorDoesNotAdvance_Throws()
    {
        var plugin = new ScriptedPanelPlugin
        {
            List = cursor => PanelAccountListResult.Page([new PanelAccount("x" + cursor, null, PanelAccountStatus.Active, null, null)], cursor ?? "same"),
        };

        var run = () => ProvisioningContract.AssertListAccountsIsCompleteAsync(plugin, PanelFixtures.Server);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("cursor");
    }

    /// <summary>A listing that never ends is cut off at the page limit.</summary>
    [Fact]
    public async Task AssertList_NeverEnds_ThrowsAtLimit()
    {
        var plugin = new ScriptedPanelPlugin
        {
            List = cursor => PanelAccountListResult.Page([new PanelAccount("a" + cursor, null, PanelAccountStatus.Active, null, null)], (cursor ?? string.Empty) + "n"),
        };

        var run = () => ProvisioningContract.AssertListAccountsIsCompleteAsync(plugin, PanelFixtures.Server, maxPages: 5);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("5 pages");
    }

    /// <summary>A listing that is refused or not supported is reported, because the caller arranged a panel that lists.</summary>
    [Fact]
    public async Task AssertList_NotSupported_Throws()
    {
        var plugin = new ScriptedPanelPlugin { List = _ => PanelAccountListResult.NotSupported() };

        var run = () => ProvisioningContract.AssertListAccountsIsCompleteAsync(plugin, PanelFixtures.Server);

        await run.Should().ThrowAsync<PluginContractViolationException>();
    }

    /// <summary>An account with a blank reference or an undefined status is rejected.</summary>
    [Fact]
    public async Task AssertList_MalformedAccount_Throws()
    {
        var blank = new ScriptedPanelPlugin { List = _ => PanelAccountListResult.Page([new PanelAccount(" ", null, PanelAccountStatus.Active, null, null)], null) };
        var status = new ScriptedPanelPlugin { List = _ => PanelAccountListResult.Page([new PanelAccount("a", null, default, null, null)], null) };

        await ((Func<Task>)(() => ProvisioningContract.AssertListAccountsIsCompleteAsync(blank, PanelFixtures.Server))).Should().ThrowAsync<PluginContractViolationException>();
        await ((Func<Task>)(() => ProvisioningContract.AssertListAccountsIsCompleteAsync(status, PanelFixtures.Server))).Should().ThrowAsync<PluginContractViolationException>();
    }

    /// <summary>A printed form that holds none of the secrets passes, and one that holds a secret is rejected without repeating it.</summary>
    [Fact]
    public void AssertHidesSecrets_PrintedForm_IsChecked()
    {
        var leaking = new LeakyResult("token-secret-002");

        ProvisioningContract.AssertHidesSecrets(PanelFixtures.Create, PanelFixtures.Password, PanelFixtures.AccessHash, PanelFixtures.ApiToken, null, string.Empty);
        var run = () => ProvisioningContract.AssertHidesSecrets(leaking, PanelFixtures.ApiToken);

        run.Should().Throw<PluginContractViolationException>().Which.Message.Should().NotContain(PanelFixtures.ApiToken);
    }

    /// <summary>Builds a listing script that serves one account per page, with a numeric cursor, and ends after the last.</summary>
    /// <param name="references">The reference of the account on each page, in order.</param>
    /// <returns>The script for <see cref="ScriptedPanelPlugin.List"/>.</returns>
    private static Func<string?, PanelAccountListResult> PagesOf(params string[] references)
    {
        return cursor =>
        {
            var index = cursor is null ? 0 : int.Parse(cursor, System.Globalization.CultureInfo.InvariantCulture);
            var account = new PanelAccount(references[index], null, PanelAccountStatus.Active, null, null);
            var next = index + 1 < references.Length ? (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
            return PanelAccountListResult.Page([account], next);
        };
    }
}
