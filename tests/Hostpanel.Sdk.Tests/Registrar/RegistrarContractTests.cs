using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Registrar;
using Hostpanel.Sdk.Plugins.StoredCredentials;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Registrar;

/// <summary>Tests of <see cref="RegistrarContract"/>: it accepts a well behaved plugin and rejects each way of breaking a promise.</summary>
public sealed class RegistrarContractTests
{
    /// <summary>A registration that finds its own domain on every repeat passes, and the helper calls it as often as asked.</summary>
    [Fact]
    public async Task AssertRegister_RepeatFindsTheDomain_Passes()
    {
        var plugin = new ScriptedRegistrarPlugin();

        var first = await RegistrarContract.AssertRegisterIsIdempotentAsync(plugin, RegistrarFixtures.Register, calls: 4);

        first.Outcome.Should().Be(PanelOperationOutcome.Succeeded);
        first.Value!.RegistrarRef.Should().Be("dom-1");
        plugin.CallsOf(nameof(IRegistrarOperationsPlugin.RegisterAsync)).Should().Be(4);
    }

    /// <summary>A first call that finds a domain an earlier attempt registered also passes.</summary>
    [Fact]
    public async Task AssertRegister_FirstCallAlreadyRegistered_Passes()
    {
        var order = new RegistrarOrder("dom-1", null, false);
        var plugin = new ScriptedRegistrarPlugin { Register = _ => RegistrarDataResult<RegistrarOrder>.AlreadyDone(order) };

        var first = await RegistrarContract.AssertRegisterIsIdempotentAsync(plugin, RegistrarFixtures.Register);

        first.Outcome.Should().Be(PanelOperationOutcome.AlreadyApplied);
    }

    /// <summary>A plugin that registers again on a repeat is rejected, naming the call.</summary>
    [Fact]
    public async Task AssertRegister_RegistersTwice_Throws()
    {
        var order = new RegistrarOrder("dom-1", null, false);
        var plugin = new ScriptedRegistrarPlugin { Register = _ => RegistrarDataResult<RegistrarOrder>.Done(order) };

        var run = () => RegistrarContract.AssertRegisterIsIdempotentAsync(plugin, RegistrarFixtures.Register);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("call 2");
    }

    /// <summary>A plugin that answers another reference on a repeat is rejected.</summary>
    [Fact]
    public async Task AssertRegister_ReferenceChanges_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            Register = call => call == 1
                ? RegistrarDataResult<RegistrarOrder>.Done(new RegistrarOrder("dom-1", null, false))
                : RegistrarDataResult<RegistrarOrder>.AlreadyDone(new RegistrarOrder("dom-2", null, false)),
        };

        var run = () => RegistrarContract.AssertRegisterIsIdempotentAsync(plugin, RegistrarFixtures.Register);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("reference");
    }

    /// <summary>A success without a reference is rejected, because the host could not address the domain afterwards.</summary>
    [Fact]
    public async Task AssertRegister_NoReference_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin { Register = _ => RegistrarDataResult<RegistrarOrder>.Done(new RegistrarOrder(" ", null, false)) };

        var run = () => RegistrarContract.AssertRegisterIsIdempotentAsync(plugin, RegistrarFixtures.Register);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("without a reference");
    }

    /// <summary>A plugin that throws on the repeat is rejected, with the exception type and not its text.</summary>
    [Fact]
    public async Task AssertRegister_ThrowsOnRepeat_ThrowsWithTypeOnly()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            Register = call => call == 1
                ? RegistrarDataResult<RegistrarOrder>.Done(new RegistrarOrder("dom-1", null, false))
                : throw new InvalidOperationException("domain example.test exists on api.registrar.example.test"),
        };

        var run = () => RegistrarContract.AssertRegisterIsIdempotentAsync(plugin, RegistrarFixtures.Register);

        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain("InvalidOperationException").And.NotContain("registrar.example.test");
    }

    /// <summary>A refusal while the caller arranged a registration the registrar accepts is reported, not passed.</summary>
    [Fact]
    public async Task AssertRegister_Refused_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin { Register = _ => RegistrarDataResult<RegistrarOrder>.Refused(RegistrarRefusalReason.DomainUnavailable) };

        var run = () => RegistrarContract.AssertRegisterIsIdempotentAsync(plugin, RegistrarFixtures.Register);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("Refused");
    }

    /// <summary>A refusal without a reason, and a reason on a success, are rejected as malformed answers.</summary>
    [Fact]
    public async Task AssertRegister_MalformedReason_Throws()
    {
        var noReason = new ScriptedRegistrarPlugin { Register = _ => new RegistrarDataResult<RegistrarOrder>(PanelOperationOutcome.Refused, null, null) };
        var strayReason = new ScriptedRegistrarPlugin
        {
            Register = _ => new RegistrarDataResult<RegistrarOrder>(PanelOperationOutcome.Succeeded, new RegistrarOrder("dom-1", null, false), RegistrarRefusalReason.Other),
        };

        var missing = () => RegistrarContract.AssertRegisterIsIdempotentAsync(noReason, RegistrarFixtures.Register);
        var stray = () => RegistrarContract.AssertRegisterIsIdempotentAsync(strayReason, RegistrarFixtures.Register);

        (await missing.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("RegistrarRefusalReason");
        (await stray.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("only a refusal has one");
    }

    /// <summary>A plugin that answers null is rejected.</summary>
    [Fact]
    public async Task AssertRegister_AnswersNull_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin { Register = _ => null! };

        var run = () => RegistrarContract.AssertRegisterIsIdempotentAsync(plugin, RegistrarFixtures.Register);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("null");
    }

    /// <summary>Asking for fewer than two calls is a mistake of the test, not of the plugin.</summary>
    [Fact]
    public async Task AssertRegister_FewerThanTwoCalls_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin();

        var run = () => RegistrarContract.AssertRegisterIsIdempotentAsync(plugin, RegistrarFixtures.Register, calls: 1);

        await run.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>A transfer that finds the one under way on every repeat passes, and one that starts a second is rejected.</summary>
    [Fact]
    public async Task AssertTransfer_IsChecked()
    {
        var good = new ScriptedRegistrarPlugin();
        var twice = new ScriptedRegistrarPlugin { Transfer = _ => RegistrarDataResult<RegistrarOrder>.Done(new RegistrarOrder("dom-1", null, true)) };

        var first = await RegistrarContract.AssertTransferIsIdempotentAsync(good, RegistrarFixtures.Transfer);
        var run = () => RegistrarContract.AssertTransferIsIdempotentAsync(twice, RegistrarFixtures.Transfer);

        first.Outcome.Should().Be(PanelOperationOutcome.Succeeded);
        await run.Should().ThrowAsync<PluginContractViolationException>();
    }

    /// <summary>A renewal that notices the expiry already moved passes; one that renews twice is rejected.</summary>
    [Fact]
    public async Task AssertRenew_IsChecked()
    {
        var good = new ScriptedRegistrarPlugin();
        var twice = new ScriptedRegistrarPlugin { Renew = _ => RegistrarDataResult<RegistrarOrder>.Done(new RegistrarOrder("dom-1", null, false)) };

        var first = await RegistrarContract.AssertRenewIsIdempotentAsync(good, RegistrarFixtures.Renew);
        var run = () => RegistrarContract.AssertRenewIsIdempotentAsync(twice, RegistrarFixtures.Renew);

        first.Outcome.Should().Be(PanelOperationOutcome.Succeeded);
        await run.Should().ThrowAsync<PluginContractViolationException>();
    }

    /// <summary>A renewal context without the host's expiry cannot be repeated safely, so the check refuses to run it.</summary>
    [Fact]
    public async Task AssertRenew_WithoutCurrentExpiry_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin();

        var run = () => RegistrarContract.AssertRenewIsIdempotentAsync(plugin, RegistrarFixtures.Renew with { CurrentExpiresAt = null });

        await run.Should().ThrowAsync<ArgumentException>();
        plugin.CallsOf(nameof(IRegistrarOperationsPlugin.RenewAsync)).Should().Be(0);
    }

    /// <summary>Every change helper passes a plugin whose repeat is already applied, and counts the calls it made.</summary>
    [Fact]
    public async Task AssertChanges_WellBehavedPlugin_Pass()
    {
        var plugin = new ScriptedRegistrarPlugin();

        await RegistrarContract.AssertSetNameserversIsIdempotentAsync(plugin, RegistrarFixtures.Nameservers);
        await RegistrarContract.AssertSetAutoRenewIsIdempotentAsync(plugin, RegistrarFixtures.Flag);
        await RegistrarContract.AssertSetWhoisPrivacyIsIdempotentAsync(plugin, RegistrarFixtures.Flag);
        await RegistrarContract.AssertSetRegistrarLockIsIdempotentAsync(plugin, RegistrarFixtures.Flag);
        await RegistrarContract.AssertSetContactIsIdempotentAsync(plugin, RegistrarFixtures.ContactChange);
        await RegistrarContract.AssertDeleteDnsRecordIsIdempotentAsync(plugin, RegistrarFixtures.Removal);

        plugin.CallsOf(nameof(IRegistrarOperationsPlugin.SetNameserversAsync)).Should().Be(3);
        plugin.CallsOf(nameof(IRegistrarOperationsPlugin.DeleteDnsRecordAsync)).Should().Be(3);
    }

    /// <summary>A change that answers not found on a repeat is rejected, because the wanted state was reached.</summary>
    [Fact]
    public async Task AssertChanges_NotFoundOnRepeat_Throw()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            SetNameservers = DoneThenNotFound,
            SetAutoRenew = DoneThenNotFound,
            SetWhoisPrivacy = DoneThenNotFound,
            SetRegistrarLock = DoneThenNotFound,
            SetContact = DoneThenNotFound,
            DeleteDnsRecord = DoneThenNotFound,
        };

        var runs = new Func<Task>[]
        {
            () => RegistrarContract.AssertSetNameserversIsIdempotentAsync(plugin, RegistrarFixtures.Nameservers),
            () => RegistrarContract.AssertSetAutoRenewIsIdempotentAsync(plugin, RegistrarFixtures.Flag),
            () => RegistrarContract.AssertSetWhoisPrivacyIsIdempotentAsync(plugin, RegistrarFixtures.Flag),
            () => RegistrarContract.AssertSetRegistrarLockIsIdempotentAsync(plugin, RegistrarFixtures.Flag),
            () => RegistrarContract.AssertSetContactIsIdempotentAsync(plugin, RegistrarFixtures.ContactChange),
            () => RegistrarContract.AssertDeleteDnsRecordIsIdempotentAsync(plugin, RegistrarFixtures.Removal),
        };

        foreach (var run in runs)
        {
            await run.Should().ThrowAsync<PluginContractViolationException>();
        }
    }

    /// <summary>A registrar that holds no such domain is answered not found everywhere it must be, and a plugin that does so passes.</summary>
    [Fact]
    public async Task AssertUnknownDomain_WellBehavedPlugin_Passes()
    {
        var plugin = new ScriptedRegistrarPlugin { HoldsDomain = false };

        await RegistrarContract.AssertUnknownDomainIsNotFoundAsync(plugin, RegistrarFixtures.DomainContext);

        plugin.CallsOf(nameof(IRegistrarOperationsPlugin.GetNameserversAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IRegistrarOperationsPlugin.InitiateOutgoingTransferAsync)).Should().Be(1);
    }

    /// <summary>A plugin that has no authorization code or no DNS hosting may say so instead of not found.</summary>
    [Fact]
    public async Task AssertUnknownDomain_NotSupportedWhereAllowed_Passes()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            HoldsDomain = false,
            GetEppCode = _ => RegistrarDataResult<RegistrarEppCode>.NotSupported(),
            GetDnsRecords = _ => RegistrarDataResult<IReadOnlyList<RegistrarDnsRecord>>.NotSupported(),
        };

        await RegistrarContract.AssertUnknownDomainIsNotFoundAsync(plugin, RegistrarFixtures.DomainContext);
    }

    /// <summary>A plugin that changes a domain it does not hold is rejected, naming the operation.</summary>
    [Fact]
    public async Task AssertUnknownDomain_ChangesUnknownDomain_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin { HoldsDomain = false, SetNameservers = _ => RegistrarActionResult.Done() };

        var run = () => RegistrarContract.AssertUnknownDomainIsNotFoundAsync(plugin, RegistrarFixtures.DomainContext);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("SetNameserversAsync").And.Contain("unknown domain");
    }

    /// <summary>A plugin that refuses instead of answering not found for an unknown domain is rejected.</summary>
    [Fact]
    public async Task AssertUnknownDomain_RefusesInsteadOfNotFound_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            HoldsDomain = false,
            Renew = _ => RegistrarDataResult<RegistrarOrder>.Refused(RegistrarRefusalReason.Other),
        };

        var run = () => RegistrarContract.AssertUnknownDomainIsNotFoundAsync(plugin, RegistrarFixtures.DomainContext);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("RenewAsync");
    }

    /// <summary>A connection test passes whatever outcome the plugin answers, as long as the result is well formed.</summary>
    [Fact]
    public async Task AssertTestConnection_WellFormedAnswers_Pass()
    {
        var connected = new ScriptedRegistrarPlugin();
        var unreachable = new ScriptedRegistrarPlugin { Test = _ => PanelConnectionTestResult.Failed(PanelConnectionOutcome.Unreachable) };

        var good = await RegistrarContract.AssertTestConnectionAnswersAsync(connected, RegistrarFixtures.Test);
        var bad = await RegistrarContract.AssertTestConnectionAnswersAsync(unreachable, RegistrarFixtures.Test);

        good.Outcome.Should().Be(PanelConnectionOutcome.Connected);
        bad.Outcome.Should().Be(PanelConnectionOutcome.Unreachable);
    }

    /// <summary>A connection test that throws, or that carries a count with a failure, is rejected.</summary>
    [Fact]
    public async Task AssertTestConnection_BrokenAnswers_Throw()
    {
        var throws = new ScriptedRegistrarPlugin { Test = _ => throw new HttpRequestException("api.registrar.example.test refused") };
        var count = new ScriptedRegistrarPlugin { Test = _ => new PanelConnectionTestResult(PanelConnectionOutcome.AuthenticationFailed, null, 4) };
        var negative = new ScriptedRegistrarPlugin { Test = _ => new PanelConnectionTestResult(PanelConnectionOutcome.Connected, null, -1) };

        var thrown = () => RegistrarContract.AssertTestConnectionAnswersAsync(throws, RegistrarFixtures.Test);
        var withCount = () => RegistrarContract.AssertTestConnectionAnswersAsync(count, RegistrarFixtures.Test);
        var withNegative = () => RegistrarContract.AssertTestConnectionAnswersAsync(negative, RegistrarFixtures.Test);

        (await thrown.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("HttpRequestException").And.NotContain("registrar.example.test");
        await withCount.Should().ThrowAsync<PluginContractViolationException>();
        await withNegative.Should().ThrowAsync<PluginContractViolationException>();
    }

    /// <summary>A printed form that holds none of the secrets passes, and one that holds a secret is rejected without repeating it.</summary>
    [Fact]
    public void AssertHidesSecrets_PrintedForm_IsChecked()
    {
        var leaking = new LeakyResult(RegistrarFixtures.EppCode);

        RegistrarContract.AssertHidesSecrets(RegistrarFixtures.Transfer, RegistrarFixtures.ApiKey, RegistrarFixtures.EppCode, null, string.Empty);
        var run = () => RegistrarContract.AssertHidesSecrets(leaking, RegistrarFixtures.EppCode);

        run.Should().Throw<PluginContractViolationException>().Which.Message.Should().NotContain(RegistrarFixtures.EppCode);
    }

    /// <summary>The helpers refuse a missing plugin or context with an argument error rather than a null reference.</summary>
    [Fact]
    public async Task Helpers_NullArguments_Throw()
    {
        var plugin = new ScriptedRegistrarPlugin();

        var noPlugin = () => RegistrarContract.AssertRegisterIsIdempotentAsync(null!, RegistrarFixtures.Register);
        var noContext = () => RegistrarContract.AssertRegisterIsIdempotentAsync(plugin, null!);
        var noSecrets = () => RegistrarContract.AssertHidesSecrets(RegistrarFixtures.Transfer, null!);

        await noPlugin.Should().ThrowAsync<ArgumentNullException>();
        await noContext.Should().ThrowAsync<ArgumentNullException>();
        noSecrets.Should().Throw<ArgumentNullException>();
    }

    /// <summary>A script for a change that is done the first time and then answers not found, which breaks the idempotency promise.</summary>
    /// <param name="call">The call number, starting at 1.</param>
    /// <returns>The answer of that call.</returns>
    private static RegistrarActionResult DoneThenNotFound(int call)
    {
        return call == 1 ? RegistrarActionResult.Done() : RegistrarActionResult.NotFound();
    }

    /// <summary>An object whose printed form leaks the value it was given, to show the secret check catches it.</summary>
    /// <param name="Value">The value the printed form shows.</param>
    private sealed record LeakyResult(string Value);
}
