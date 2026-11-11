using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.StoredCredentials;
using Hostpanel.Sdk.Plugins.VirtualServers;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.VirtualServers;

/// <summary>Tests of <see cref="VirtualServerContract"/>: it accepts a well behaved plugin and rejects each way of breaking a promise.</summary>
public sealed class VirtualServerContractTests
{
    /// <summary>A create that finds its own server on every repeat passes, and the helper calls it as often as asked.</summary>
    [Fact]
    public async Task AssertCreate_RepeatFindsTheServer_Passes()
    {
        var plugin = new ScriptedVirtualServerPlugin();

        var first = await VirtualServerContract.AssertCreateIsIdempotentAsync(plugin, VirtualServerFixtures.Create, calls: 4);

        first.Outcome.Should().Be(PanelOperationOutcome.Succeeded);
        first.Value!.ProvisioningRef.Should().Be("srv-1");
        plugin.CallsOf(nameof(IVirtualServerPlugin.CreateServerAsync)).Should().Be(4);
    }

    /// <summary>A plugin that creates the server again on a repeat is rejected, naming the call.</summary>
    [Fact]
    public async Task AssertCreate_CreatesTwice_Throws()
    {
        var plugin = new ScriptedVirtualServerPlugin
        {
            Create = _ => ValueResult<VirtualServerInstance, PanelRefusalReason>.Done(new VirtualServerInstance("srv-1", null, null, null)),
        };

        var run = () => VirtualServerContract.AssertCreateIsIdempotentAsync(plugin, VirtualServerFixtures.Create);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("call 2");
    }

    /// <summary>A plugin that answers another reference on a repeat is rejected.</summary>
    [Fact]
    public async Task AssertCreate_ReferenceChanges_Throws()
    {
        var plugin = new ScriptedVirtualServerPlugin
        {
            Create = call => call == 1
                ? ValueResult<VirtualServerInstance, PanelRefusalReason>.Done(new VirtualServerInstance("srv-1", null, null, null))
                : ValueResult<VirtualServerInstance, PanelRefusalReason>.AlreadyDone(new VirtualServerInstance("srv-2", null, null, null)),
        };

        var run = () => VirtualServerContract.AssertCreateIsIdempotentAsync(plugin, VirtualServerFixtures.Create);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("reference");
    }

    /// <summary>A refusal while the caller arranged a create the platform accepts is reported, not passed.</summary>
    [Fact]
    public async Task AssertCreate_Refused_Throws()
    {
        var plugin = new ScriptedVirtualServerPlugin
        {
            Create = _ => ValueResult<VirtualServerInstance, PanelRefusalReason>.Refused(PanelRefusalReason.ImageUnknown),
        };

        var run = () => VirtualServerContract.AssertCreateIsIdempotentAsync(plugin, VirtualServerFixtures.Create);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("Refused");
    }

    /// <summary>A plugin that throws on the repeat is rejected, with the exception type and not its text.</summary>
    [Fact]
    public async Task AssertCreate_ThrowsOnRepeat_ThrowsWithTypeOnly()
    {
        var plugin = new ScriptedVirtualServerPlugin
        {
            Create = call => call == 1
                ? ValueResult<VirtualServerInstance, PanelRefusalReason>.Done(new VirtualServerInstance("srv-1", null, null, null))
                : throw new InvalidOperationException("server vps-42 exists on cloud.example.test"),
        };

        var run = () => VirtualServerContract.AssertCreateIsIdempotentAsync(plugin, VirtualServerFixtures.Create);

        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain("InvalidOperationException").And.NotContain("cloud.example.test");
    }

    /// <summary>Every idempotent change helper passes a plugin whose repeat is already applied, and counts the calls it made.</summary>
    [Fact]
    public async Task AssertChanges_WellBehavedPlugin_Pass()
    {
        var plugin = new ScriptedVirtualServerPlugin();

        await VirtualServerContract.AssertStartIsIdempotentAsync(plugin, PanelFixtures.Account);
        await VirtualServerContract.AssertStopIsIdempotentAsync(plugin, PanelFixtures.Account, calls: 2);
        await VirtualServerContract.AssertSuspendIsIdempotentAsync(plugin, PanelFixtures.Suspend);
        await VirtualServerContract.AssertUnsuspendIsIdempotentAsync(plugin, PanelFixtures.Account);
        await VirtualServerContract.AssertTerminateIsIdempotentAsync(plugin, PanelFixtures.Terminate);
        await VirtualServerContract.AssertChangePlanIsIdempotentAsync(plugin, PanelFixtures.ChangePackage);

        plugin.CallsOf(nameof(IVirtualServerPlugin.StartServerAsync)).Should().Be(3);
        plugin.CallsOf(nameof(IVirtualServerPlugin.StopServerAsync)).Should().Be(2);
        plugin.CallsOf(nameof(IVirtualServerPlugin.ChangePlanAsync)).Should().Be(3);
    }

    /// <summary>A change that answers not found on a repeat is rejected, because the wanted state was reached.</summary>
    [Fact]
    public async Task AssertChanges_NotFoundOnRepeat_Throw()
    {
        var plugin = new ScriptedVirtualServerPlugin
        {
            Start = DoneThenNotFound,
            Stop = DoneThenNotFound,
            Suspend = DoneThenNotFound,
            Unsuspend = DoneThenNotFound,
            Terminate = DoneThenNotFound,
            ChangePlan = DoneThenNotFound,
        };

        var runs = new Func<Task>[]
        {
            () => VirtualServerContract.AssertStartIsIdempotentAsync(plugin, PanelFixtures.Account),
            () => VirtualServerContract.AssertStopIsIdempotentAsync(plugin, PanelFixtures.Account),
            () => VirtualServerContract.AssertSuspendIsIdempotentAsync(plugin, PanelFixtures.Suspend),
            () => VirtualServerContract.AssertUnsuspendIsIdempotentAsync(plugin, PanelFixtures.Account),
            () => VirtualServerContract.AssertTerminateIsIdempotentAsync(plugin, PanelFixtures.Terminate),
            () => VirtualServerContract.AssertChangePlanIsIdempotentAsync(plugin, PanelFixtures.ChangePackage),
        };

        foreach (var run in runs)
        {
            await run.Should().ThrowAsync<PluginContractViolationException>();
        }
    }

    /// <summary>Asking for fewer than two calls is a mistake of the test, not of the plugin.</summary>
    [Fact]
    public async Task AssertStart_FewerThanTwoCalls_Throws()
    {
        var plugin = new ScriptedVirtualServerPlugin();

        var run = () => VirtualServerContract.AssertStartIsIdempotentAsync(plugin, PanelFixtures.Account, calls: 1);

        await run.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>A platform that holds no such server is answered not found everywhere it must be, and a plugin that does so passes.</summary>
    [Fact]
    public async Task AssertUnknownServer_WellBehavedPlugin_Passes()
    {
        var plugin = new ScriptedVirtualServerPlugin { HoldsServer = false };

        await VirtualServerContract.AssertUnknownServerIsNotFoundAsync(plugin, PanelFixtures.Account, new PanelPackage("small"), "img-1");

        plugin.CallsOf(nameof(IVirtualServerPlugin.StartServerAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IVirtualServerPlugin.TerminateServerAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IVirtualServerPlugin.ResetRootPasswordAsync)).Should().Be(1);
    }

    /// <summary>A plugin with no console may say so instead of not found.</summary>
    [Fact]
    public async Task AssertUnknownServer_ConsoleNotSupported_Passes()
    {
        var plugin = new ScriptedVirtualServerPlugin
        {
            HoldsServer = false,
            ConsoleSession = _ => ValueResult<VirtualServerConsole, PanelRefusalReason>.NotSupported(),
        };

        await VirtualServerContract.AssertUnknownServerIsNotFoundAsync(plugin, PanelFixtures.Account, new PanelPackage("small"), "img-1");
    }

    /// <summary>A plugin that starts a server it does not hold is rejected, naming the operation.</summary>
    [Fact]
    public async Task AssertUnknownServer_StartsUnknownServer_Throws()
    {
        var plugin = new ScriptedVirtualServerPlugin { HoldsServer = false, Start = _ => PanelActionResult.Done() };

        var run = () => VirtualServerContract.AssertUnknownServerIsNotFoundAsync(plugin, PanelFixtures.Account, new PanelPackage("small"), "img-1");

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("StartServerAsync").And.Contain("unknown server");
    }

    /// <summary>A plugin that answers a success for the termination of a server it does not hold is rejected: only already applied fits.</summary>
    [Fact]
    public async Task AssertUnknownServer_TerminateSucceeded_Throws()
    {
        var plugin = new ScriptedVirtualServerPlugin { HoldsServer = false, Terminate = _ => PanelActionResult.Done() };

        var run = () => VirtualServerContract.AssertUnknownServerIsNotFoundAsync(plugin, PanelFixtures.Account, new PanelPackage("small"), "img-1");

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("TerminateServerAsync").And.Contain("AlreadyApplied");
    }

    /// <summary>A start of a suspended server is refused with access denied, and a plugin that starts it anyway is rejected.</summary>
    [Fact]
    public async Task AssertSuspendedServer_IsNotStarted()
    {
        var good = new ScriptedVirtualServerPlugin { IsSuspended = true };
        var starts = new ScriptedVirtualServerPlugin { IsSuspended = true, Start = _ => PanelActionResult.Done() };
        var other = new ScriptedVirtualServerPlugin { Start = _ => PanelActionResult.Refused(PanelRefusalReason.Other) };

        await VirtualServerContract.AssertSuspendedServerIsNotStartedAsync(good, PanelFixtures.Account);
        var startsRun = () => VirtualServerContract.AssertSuspendedServerIsNotStartedAsync(starts, PanelFixtures.Account);
        var otherRun = () => VirtualServerContract.AssertSuspendedServerIsNotStartedAsync(other, PanelFixtures.Account);

        (await startsRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("AccessDenied");
        await otherRun.Should().ThrowAsync<PluginContractViolationException>();
    }

    /// <summary>The reads of a held server answer values, the console may be not supported, and a success without a value is rejected.</summary>
    [Fact]
    public async Task AssertReads_AreChecked()
    {
        var good = new ScriptedVirtualServerPlugin();
        var noConsole = new ScriptedVirtualServerPlugin { ConsoleSession = _ => ValueResult<VirtualServerConsole, PanelRefusalReason>.NotSupported() };
        var empty = new ScriptedVirtualServerPlugin
        {
            ConsoleSession = _ => new ValueResult<VirtualServerConsole, PanelRefusalReason>(PanelOperationOutcome.Succeeded, null, null),
        };

        await VirtualServerContract.AssertReadsAnswerValuesAsync(good, PanelFixtures.Account);
        await VirtualServerContract.AssertReadsAnswerValuesAsync(noConsole, PanelFixtures.Account);
        var run = () => VirtualServerContract.AssertReadsAnswerValuesAsync(empty, PanelFixtures.Account);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("CreateConsoleSessionAsync").And.Contain("without a value");
    }

    /// <summary>The server listing follows the cursors to the end, and a duplicate or a cursor that does not move is rejected.</summary>
    [Fact]
    public async Task AssertListServers_IsChecked()
    {
        var good = new ScriptedVirtualServerPlugin { ListServers = PagesOf("a", "b", "c") };
        var twice = new ScriptedVirtualServerPlugin { ListServers = PagesOf("a", "b", "a") };
        var stuck = new ScriptedVirtualServerPlugin
        {
            ListServers = cursor => PanelAccountListResult.Page([new PanelAccount("x" + cursor, null, PanelAccountStatus.Active, null, null)], cursor ?? "same"),
        };

        var servers = await VirtualServerContract.AssertListServersIsCompleteAsync(good, PanelFixtures.Server);
        var twiceRun = () => VirtualServerContract.AssertListServersIsCompleteAsync(twice, PanelFixtures.Server);
        var stuckRun = () => VirtualServerContract.AssertListServersIsCompleteAsync(stuck, PanelFixtures.Server);

        servers.Select(server => server.ProvisioningRef).Should().Equal("a", "b", "c");
        (await twiceRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("ListServersAsync").And.Contain("twice");
        (await stuckRun.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("cursor");
    }

    /// <summary>A plugin that issues a console address for a server it does not hold is rejected.</summary>
    [Fact]
    public async Task AssertUnknownServer_ConsoleForUnknownServer_Throws()
    {
        var plugin = new ScriptedVirtualServerPlugin
        {
            HoldsServer = false,
            ConsoleSession = _ => ValueResult<VirtualServerConsole, PanelRefusalReason>.Done(new VirtualServerConsole("https://console.example.test/s", null)),
        };

        var run = () => VirtualServerContract.AssertUnknownServerIsNotFoundAsync(plugin, PanelFixtures.Account, new PanelPackage("small"), "img-1");

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("CreateConsoleSessionAsync");
    }

    /// <summary>Two well formed catalogues pass, and both lists are read once.</summary>
    [Fact]
    public async Task AssertCatalog_WellFormed_Passes()
    {
        var plugin = new ScriptedVirtualServerPlugin();

        await VirtualServerContract.AssertCatalogIsWellFormedAsync(plugin, VirtualServerFixtures.Catalog);

        plugin.CallsOf(nameof(IVirtualServerPlugin.ListImagesAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IVirtualServerPlugin.ListPlansAsync)).Should().Be(1);
    }

    /// <summary>A catalogue with a repeated image id, a blank image or a repeated plan name is rejected.</summary>
    [Fact]
    public async Task AssertCatalog_Malformed_Throws()
    {
        var twiceImage = new ScriptedVirtualServerPlugin
        {
            ListImages = _ => ValueResult<IReadOnlyList<VirtualServerImage>, PanelRefusalReason>.Done([new VirtualServerImage("a", "A"), new VirtualServerImage("a", "A again")]),
        };
        var blankImage = new ScriptedVirtualServerPlugin
        {
            ListImages = _ => ValueResult<IReadOnlyList<VirtualServerImage>, PanelRefusalReason>.Done([new VirtualServerImage(" ", "A")]),
        };
        var twicePlan = new ScriptedVirtualServerPlugin
        {
            ListPlans = _ => ValueResult<IReadOnlyList<VirtualServerPlan>, PanelRefusalReason>.Done([new VirtualServerPlan("s", null, null, null), new VirtualServerPlan("s", null, null, null)]),
        };

        var runs = new Func<Task>[]
        {
            () => VirtualServerContract.AssertCatalogIsWellFormedAsync(twiceImage, VirtualServerFixtures.Catalog),
            () => VirtualServerContract.AssertCatalogIsWellFormedAsync(blankImage, VirtualServerFixtures.Catalog),
            () => VirtualServerContract.AssertCatalogIsWellFormedAsync(twicePlan, VirtualServerFixtures.Catalog),
        };

        foreach (var run in runs)
        {
            await run.Should().ThrowAsync<PluginContractViolationException>();
        }
    }

    /// <summary>A catalogue that is not supported is reported, because the caller arranged a platform that lists.</summary>
    [Fact]
    public async Task AssertCatalog_NotSupported_Throws()
    {
        var plugin = new ScriptedVirtualServerPlugin { ListPlans = _ => ValueResult<IReadOnlyList<VirtualServerPlan>, PanelRefusalReason>.NotSupported() };

        var run = () => VirtualServerContract.AssertCatalogIsWellFormedAsync(plugin, VirtualServerFixtures.Catalog);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("ListPlansAsync");
    }

    /// <summary>A catalogue that succeeds without a list is rejected: an empty catalogue is an empty list.</summary>
    [Fact]
    public async Task AssertCatalog_SuccessWithoutList_Throws()
    {
        var plugin = new ScriptedVirtualServerPlugin
        {
            ListImages = _ => new ValueResult<IReadOnlyList<VirtualServerImage>, PanelRefusalReason>(PanelOperationOutcome.Succeeded, null, null),
        };

        var run = () => VirtualServerContract.AssertCatalogIsWellFormedAsync(plugin, VirtualServerFixtures.Catalog);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("without a list");
    }

    /// <summary>A connection test passes whatever outcome the plugin answers, as long as the result is well formed.</summary>
    [Fact]
    public async Task AssertTestConnection_WellFormedAnswers_Pass()
    {
        var connected = new ScriptedVirtualServerPlugin();
        var failed = new ScriptedVirtualServerPlugin { Test = _ => PanelConnectionTestResult.Failed(PanelConnectionOutcome.AuthenticationFailed) };

        var good = await VirtualServerContract.AssertTestConnectionAnswersAsync(connected, new PanelTestConnectionContext(PanelFixtures.Server));
        var bad = await VirtualServerContract.AssertTestConnectionAnswersAsync(failed, new PanelTestConnectionContext(PanelFixtures.Server));

        good.Outcome.Should().Be(PanelConnectionOutcome.Connected);
        bad.Outcome.Should().Be(PanelConnectionOutcome.AuthenticationFailed);
    }

    /// <summary>A connection test that throws is rejected, with the exception type and not its text.</summary>
    [Fact]
    public async Task AssertTestConnection_Throws_ThrowsWithTypeOnly()
    {
        var plugin = new ScriptedVirtualServerPlugin { Test = _ => throw new HttpRequestException("cloud.example.test refused") };

        var run = () => VirtualServerContract.AssertTestConnectionAnswersAsync(plugin, new PanelTestConnectionContext(PanelFixtures.Server));

        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain("HttpRequestException").And.NotContain("cloud.example.test");
    }

    /// <summary>A printed form that holds none of the secrets passes, and one that holds a secret is rejected without repeating it.</summary>
    [Fact]
    public void AssertHidesSecrets_PrintedForm_IsChecked()
    {
        var leaking = new LeakyResult(VirtualServerFixtures.RootPassword);

        VirtualServerContract.AssertHidesSecrets(VirtualServerFixtures.Create, VirtualServerFixtures.RootPassword, PanelFixtures.ApiToken, null, string.Empty);
        var run = () => VirtualServerContract.AssertHidesSecrets(leaking, VirtualServerFixtures.RootPassword);

        run.Should().Throw<PluginContractViolationException>().Which.Message.Should().NotContain(VirtualServerFixtures.RootPassword);
    }

    /// <summary>The helpers refuse a missing plugin or context with an argument error rather than a null reference.</summary>
    [Fact]
    public async Task Helpers_NullArguments_Throw()
    {
        var plugin = new ScriptedVirtualServerPlugin();

        var noPlugin = () => VirtualServerContract.AssertCreateIsIdempotentAsync(null!, VirtualServerFixtures.Create);
        var noContext = () => VirtualServerContract.AssertCreateIsIdempotentAsync(plugin, null!);

        await noPlugin.Should().ThrowAsync<ArgumentNullException>();
        await noContext.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>Builds a listing script that serves one server per page, with a numeric cursor, and ends after the last.</summary>
    /// <param name="references">The reference of the server on each page, in order.</param>
    /// <returns>The script for <see cref="ScriptedVirtualServerPlugin.ListServers"/>.</returns>
    private static Func<string?, PanelAccountListResult> PagesOf(params string[] references)
    {
        return cursor =>
        {
            var index = cursor is null ? 0 : int.Parse(cursor, System.Globalization.CultureInfo.InvariantCulture);
            var server = new PanelAccount(references[index], null, PanelAccountStatus.Active, null, null);
            var next = index + 1 < references.Length ? (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
            return PanelAccountListResult.Page([server], next);
        };
    }

    /// <summary>A script for a change that is done the first time and then answers not found, which breaks the idempotency promise.</summary>
    /// <param name="call">The call number, starting at 1.</param>
    /// <returns>The answer of that call.</returns>
    private static PanelActionResult DoneThenNotFound(int call)
    {
        return call == 1 ? PanelActionResult.Done() : PanelActionResult.NotFound();
    }

    /// <summary>An object whose printed form leaks the value it was given, to show the secret check catches it.</summary>
    /// <param name="Value">The value the printed form shows.</param>
    private sealed record LeakyResult(string Value);
}
