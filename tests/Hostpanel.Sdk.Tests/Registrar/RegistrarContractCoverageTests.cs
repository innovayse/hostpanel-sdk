using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Registrar;
using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Plugins.StoredCredentials;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Registrar;

/// <summary>Tests of the parts of <see cref="RegistrarContract"/> that cover DNS records, the optional interfaces and the reads.</summary>
public sealed class RegistrarContractCoverageTests
{
    /// <summary>A delete that finds the record gone on a repeat and answers already applied passes, and counts its calls.</summary>
    [Fact]
    public async Task AssertDeleteDnsRecord_RepeatIsAlreadyApplied_Passes()
    {
        var plugin = new ScriptedRegistrarPlugin();

        var first = await RegistrarContract.AssertDeleteDnsRecordIsIdempotentAsync(plugin, RegistrarFixtures.Removal, calls: 4);

        first.Outcome.Should().Be(PanelOperationOutcome.Succeeded);
        plugin.CallsOf(nameof(IRegistrarOperationsPlugin.DeleteDnsRecordAsync)).Should().Be(4);
    }

    /// <summary>A delete that is refused while the caller arranged a record that exists is reported, naming the call.</summary>
    [Fact]
    public async Task AssertDeleteDnsRecord_Refused_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin { DeleteDnsRecord = _ => OperationResult<RegistrarRefusalReason>.Refused(RegistrarRefusalReason.DnsRecordInvalid) };

        var run = () => RegistrarContract.AssertDeleteDnsRecordIsIdempotentAsync(plugin, RegistrarFixtures.Removal);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("Refused").And.Contain("call 1");
    }

    /// <summary>A delete that throws on the repeat is rejected with the exception type and not its text.</summary>
    [Fact]
    public async Task AssertDeleteDnsRecord_ThrowsOnRepeat_ThrowsWithTypeOnly()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            DeleteDnsRecord = call => call == 1 ? OperationResult<RegistrarRefusalReason>.Done() : throw new InvalidOperationException("record www.example.test not found"),
        };

        var run = () => RegistrarContract.AssertDeleteDnsRecordIsIdempotentAsync(plugin, RegistrarFixtures.Removal);

        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain("InvalidOperationException").And.NotContain("www.example.test");
    }

    /// <summary>A delete that answers a refusal without a reason is rejected as a malformed answer.</summary>
    [Fact]
    public async Task AssertDeleteDnsRecord_RefusalWithoutReason_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin { DeleteDnsRecord = _ => new OperationResult<RegistrarRefusalReason>(PanelOperationOutcome.Refused, null) };

        var run = () => RegistrarContract.AssertDeleteDnsRecordIsIdempotentAsync(plugin, RegistrarFixtures.Removal);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("RegistrarRefusalReason");
    }

    /// <summary>A plugin that adds the same DNS record twice is rejected, because the repeat must find it.</summary>
    [Fact]
    public async Task AssertAddDnsRecord_AnswersNotFoundOnRepeat_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin { AddDnsRecord = call => call == 1 ? OperationResult<RegistrarRefusalReason>.Done() : OperationResult<RegistrarRefusalReason>.NotFound() };

        var run = () => RegistrarContract.AssertAddDnsRecordIsIdempotentAsync(plugin, RegistrarFixtures.AddRecord);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("AddDnsRecordAsync").And.Contain("call 2");
    }

    /// <summary>The optional operations pass for a plugin that keeps their promises, and each counts its calls.</summary>
    [Fact]
    public async Task AssertOptionalOperations_WellBehavedPlugin_Pass()
    {
        var plugin = new ScriptedRegistrarPlugin();

        await RegistrarContract.AssertSetEmailForwardingIsIdempotentAsync(plugin, RegistrarFixtures.Flag);
        await RegistrarContract.AssertAddEmailForwardingRuleIsIdempotentAsync(plugin, RegistrarFixtures.ForwardingRule);
        await RegistrarContract.AssertUpdateEmailForwardingRuleIsIdempotentAsync(plugin, RegistrarFixtures.ForwardingRule);
        await RegistrarContract.AssertDeleteEmailForwardingRuleIsIdempotentAsync(plugin, RegistrarFixtures.ForwardingRemoval);
        await RegistrarContract.AssertSetDnsManagementIsIdempotentAsync(plugin, RegistrarFixtures.Flag);

        plugin.CallsOf(nameof(IRegistrarEmailForwardingPlugin.AddEmailForwardingRuleAsync)).Should().Be(3);
        plugin.CallsOf(nameof(IRegistrarDnsManagementPlugin.SetDnsManagementAsync)).Should().Be(3);
    }

    /// <summary>An optional operation that answers not found on a repeat is rejected, each one naming its method.</summary>
    [Fact]
    public async Task AssertOptionalOperations_NotFoundOnRepeat_Throw()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            SetEmailForwarding = DoneThenNotFound,
            AddEmailForwardingRule = DoneThenNotFound,
            UpdateEmailForwardingRule = DoneThenNotFound,
            DeleteEmailForwardingRule = DoneThenNotFound,
            SetDnsManagement = DoneThenNotFound,
        };

        var runs = new (string Method, Func<Task> Run)[]
        {
            (nameof(IRegistrarEmailForwardingPlugin.SetEmailForwardingAsync), () => RegistrarContract.AssertSetEmailForwardingIsIdempotentAsync(plugin, RegistrarFixtures.Flag)),
            (nameof(IRegistrarEmailForwardingPlugin.AddEmailForwardingRuleAsync), () => RegistrarContract.AssertAddEmailForwardingRuleIsIdempotentAsync(plugin, RegistrarFixtures.ForwardingRule)),
            (nameof(IRegistrarEmailForwardingPlugin.UpdateEmailForwardingRuleAsync), () => RegistrarContract.AssertUpdateEmailForwardingRuleIsIdempotentAsync(plugin, RegistrarFixtures.ForwardingRule)),
            (nameof(IRegistrarEmailForwardingPlugin.DeleteEmailForwardingRuleAsync), () => RegistrarContract.AssertDeleteEmailForwardingRuleIsIdempotentAsync(plugin, RegistrarFixtures.ForwardingRemoval)),
            (nameof(IRegistrarDnsManagementPlugin.SetDnsManagementAsync), () => RegistrarContract.AssertSetDnsManagementIsIdempotentAsync(plugin, RegistrarFixtures.Flag)),
        };

        foreach (var (method, run) in runs)
        {
            (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain(method);
        }
    }

    /// <summary>A registrar that holds the domain answers every read with a value, and the check passes.</summary>
    [Fact]
    public async Task AssertReads_WellBehavedPlugin_Passes()
    {
        var plugin = new ScriptedRegistrarPlugin();

        await RegistrarContract.AssertReadsAnswerValuesAsync(plugin, RegistrarFixtures.DomainContext);

        plugin.CallsOf(nameof(IRegistrarOperationsPlugin.GetContactAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IRegistrarOperationsPlugin.GetTldPricingAsync)).Should().Be(1);
    }

    /// <summary>The authorization code and the DNS records may be not supported, and the check still passes.</summary>
    [Fact]
    public async Task AssertReads_OptionalReadsNotSupported_Passes()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            GetEppCode = _ => ValueResult<RegistrarEppCode, RegistrarRefusalReason>.NotSupported(),
            GetDnsRecords = _ => ValueResult<IReadOnlyList<RegistrarDnsRecord>, RegistrarRefusalReason>.NotSupported(),
        };

        await RegistrarContract.AssertReadsAnswerValuesAsync(plugin, RegistrarFixtures.DomainContext);
    }

    /// <summary>A list that succeeds without a value is rejected, because an empty list is an empty list.</summary>
    [Fact]
    public async Task AssertReads_SuccessWithoutValue_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            GetNameservers = _ => new ValueResult<IReadOnlyList<string>, RegistrarRefusalReason>(PanelOperationOutcome.Succeeded, null, null),
        };

        var run = () => RegistrarContract.AssertReadsAnswerValuesAsync(plugin, RegistrarFixtures.DomainContext);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("GetNameserversAsync").And.Contain("without a value");
    }

    /// <summary>A read that is not supported where the registrar must support it is rejected.</summary>
    [Fact]
    public async Task AssertReads_RequiredReadNotSupported_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            GetNameservers = _ => ValueResult<IReadOnlyList<string>, RegistrarRefusalReason>.NotSupported(),
        };

        var run = () => RegistrarContract.AssertReadsAnswerValuesAsync(plugin, RegistrarFixtures.DomainContext);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("GetNameserversAsync").And.Contain("NotSupported");
    }

    /// <summary>A WHOIS lookup that answers a record passes, and one that succeeds without a value is rejected.</summary>
    [Fact]
    public async Task AssertWhois_IsChecked()
    {
        var good = new ScriptedRegistrarPlugin();
        var empty = new ScriptedRegistrarPlugin { GetWhois = _ => new ValueResult<RegistrarWhois, RegistrarRefusalReason>(PanelOperationOutcome.Succeeded, null, null) };

        await RegistrarContract.AssertWhoisAnswersAsync(good, RegistrarFixtures.Whois);
        var run = () => RegistrarContract.AssertWhoisAnswersAsync(empty, RegistrarFixtures.Whois);

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("GetWhoisAsync");
    }

    /// <summary>The unknown domain check covers the DNS record changes: a plugin that adds, updates or deletes on a domain it does not hold is rejected.</summary>
    [Fact]
    public async Task AssertUnknownDomain_DnsChanges_AreCovered()
    {
        var add = new ScriptedRegistrarPlugin { HoldsDomain = false, AddDnsRecord = _ => OperationResult<RegistrarRefusalReason>.Done() };
        var update = new ScriptedRegistrarPlugin { HoldsDomain = false, UpdateDnsRecord = _ => OperationResult<RegistrarRefusalReason>.Done() };
        var delete = new ScriptedRegistrarPlugin { HoldsDomain = false, DeleteDnsRecord = _ => OperationResult<RegistrarRefusalReason>.AlreadyDone() };

        var runs = new (string Method, ScriptedRegistrarPlugin Plugin)[]
        {
            (nameof(IRegistrarOperationsPlugin.AddDnsRecordAsync), add),
            (nameof(IRegistrarOperationsPlugin.UpdateDnsRecordAsync), update),
            (nameof(IRegistrarOperationsPlugin.DeleteDnsRecordAsync), delete),
        };

        foreach (var (method, plugin) in runs)
        {
            var run = () => RegistrarContract.AssertUnknownDomainIsNotFoundAsync(plugin, RegistrarFixtures.DomainContext);

            (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain(method);
        }
    }

    /// <summary>The unknown domain check also covers the optional operations the plugin implements.</summary>
    [Fact]
    public async Task AssertUnknownDomain_OptionalOperations_AreCovered()
    {
        var forwarding = new ScriptedRegistrarPlugin { HoldsDomain = false, AddEmailForwardingRule = _ => OperationResult<RegistrarRefusalReason>.Done() };
        var dns = new ScriptedRegistrarPlugin { HoldsDomain = false, SetDnsManagement = _ => OperationResult<RegistrarRefusalReason>.Done() };
        var whois = new ScriptedRegistrarPlugin
        {
            HoldsDomain = false,
            GetWhois = _ => ValueResult<RegistrarWhois, RegistrarRefusalReason>.Done(new RegistrarWhois("Example Registrar", "Registrant Name", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch)),
        };

        var runs = new (string Method, ScriptedRegistrarPlugin Plugin)[]
        {
            (nameof(IRegistrarEmailForwardingPlugin.AddEmailForwardingRuleAsync), forwarding),
            (nameof(IRegistrarDnsManagementPlugin.SetDnsManagementAsync), dns),
            (nameof(IRegistrarWhoisPlugin.GetWhoisAsync), whois),
        };

        foreach (var (method, plugin) in runs)
        {
            var run = () => RegistrarContract.AssertUnknownDomainIsNotFoundAsync(plugin, RegistrarFixtures.DomainContext);

            (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain(method);
        }

        await RegistrarContract.AssertUnknownDomainIsNotFoundAsync(new ScriptedRegistrarPlugin { HoldsDomain = false }, RegistrarFixtures.DomainContext);
    }

    /// <summary>A script for a change that is done the first time and then answers not found, which breaks the idempotency promise.</summary>
    /// <param name="call">The call number, starting at 1.</param>
    /// <returns>The answer of that call.</returns>
    private static OperationResult<RegistrarRefusalReason> DoneThenNotFound(int call)
    {
        return call == 1 ? OperationResult<RegistrarRefusalReason>.Done() : OperationResult<RegistrarRefusalReason>.NotFound();
    }
}
