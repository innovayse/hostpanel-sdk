using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Registrar;
using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Plugins.StoredCredentials;
using Hostpanel.Sdk.Tests.Fakes;
using static Hostpanel.Sdk.Tests.Support.ContractViolationAsserts;

namespace Hostpanel.Sdk.Tests.Registrar;

/// <summary>Tests of the parts of <see cref="RegistrarContract"/> that check DNS records, the optional interfaces (e-mail forwarding, DNS management, WHOIS) and the reads.</summary>
public sealed class RegistrarDnsAndOptionalContractTests
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

        await AssertViolationAsync(run, "Refused", "call 1");
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

        var message = await ViolationMessageAsync(run);
        message.Should().Contain("InvalidOperationException").And.NotContain("www.example.test");
    }

    /// <summary>A delete that answers a refusal without a reason is rejected as a malformed answer.</summary>
    [Fact]
    public async Task AssertDeleteDnsRecord_RefusalWithoutReason_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin { DeleteDnsRecord = _ => new OperationResult<RegistrarRefusalReason>(PanelOperationOutcome.Refused, null) };

        var run = () => RegistrarContract.AssertDeleteDnsRecordIsIdempotentAsync(plugin, RegistrarFixtures.Removal);

        await AssertViolationAsync(run, "RegistrarRefusalReason");
    }

    /// <summary>A plugin that adds the same DNS record twice is rejected, because the repeat must find it.</summary>
    [Fact]
    public async Task AssertAddDnsRecord_AnswersNotFoundOnRepeat_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin { AddDnsRecord = call => call == 1 ? OperationResult<RegistrarRefusalReason>.Done() : OperationResult<RegistrarRefusalReason>.NotFound() };

        var run = () => RegistrarContract.AssertAddDnsRecordIsIdempotentAsync(plugin, RegistrarFixtures.AddRecord);

        await AssertViolationAsync(run, "AddDnsRecordAsync", "call 2");
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
            SetEmailForwarding = ScriptedAnswers.RegistrarDoneThenNotFound,
            AddEmailForwardingRule = ScriptedAnswers.RegistrarDoneThenNotFound,
            UpdateEmailForwardingRule = ScriptedAnswers.RegistrarDoneThenNotFound,
            DeleteEmailForwardingRule = ScriptedAnswers.RegistrarDoneThenNotFound,
            SetDnsManagement = ScriptedAnswers.RegistrarDoneThenNotFound,
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
            await AssertViolationAsync(run, method, "call 2");
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

    /// <summary>The availability, the price list, the authorization code and the DNS records may be not supported, as the interface allows, and the check still passes.</summary>
    [Fact]
    public async Task AssertReads_OptionalReadsNotSupported_Passes()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            CheckAvailability = _ => ValueResult<RegistrarAvailability, RegistrarRefusalReason>.NotSupported(),
            GetTldPricing = _ => ValueResult<IReadOnlyList<RegistrarTldPricing>, RegistrarRefusalReason>.NotSupported(),
            GetEppCode = _ => ValueResult<RegistrarEppCode, RegistrarRefusalReason>.NotSupported(),
            GetDnsRecords = _ => ValueResult<IReadOnlyList<RegistrarDnsRecord>, RegistrarRefusalReason>.NotSupported(),
        };

        await RegistrarContract.AssertReadsAnswerValuesAsync(plugin, RegistrarFixtures.DomainContext);
    }

    /// <summary>A read that succeeds without a value is rejected, because a read that succeeded must answer what it read.</summary>
    [Fact]
    public async Task AssertReads_SuccessWithoutValue_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            GetContact = _ => new ValueResult<RegistrarContact, RegistrarRefusalReason>(PanelOperationOutcome.Succeeded, null, null),
        };

        var run = () => RegistrarContract.AssertReadsAnswerValuesAsync(plugin, RegistrarFixtures.DomainContext);

        await AssertViolationAsync(run, "GetContactAsync", "without a value");
    }

    /// <summary>A read that is not supported where the registrar must support it is rejected.</summary>
    [Fact]
    public async Task AssertReads_RequiredReadNotSupported_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            GetContact = _ => ValueResult<RegistrarContact, RegistrarRefusalReason>.NotSupported(),
        };

        var run = () => RegistrarContract.AssertReadsAnswerValuesAsync(plugin, RegistrarFixtures.DomainContext);

        await AssertViolationAsync(run, "GetContactAsync", "NotSupported");
    }

    /// <summary>A WHOIS lookup that answers a record passes, and one that succeeds without a value is rejected.</summary>
    [Fact]
    public async Task AssertWhois_IsChecked()
    {
        var good = new ScriptedRegistrarPlugin();
        var empty = new ScriptedRegistrarPlugin { GetWhois = _ => new ValueResult<RegistrarWhois, RegistrarRefusalReason>(PanelOperationOutcome.Succeeded, null, null) };

        await RegistrarContract.AssertWhoisAnswersAsync(good, RegistrarFixtures.Whois);
        var run = () => RegistrarContract.AssertWhoisAnswersAsync(empty, RegistrarFixtures.Whois);

        await AssertViolationAsync(run, "GetWhoisAsync");
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

            await AssertViolationAsync(run, method);
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

            await AssertViolationAsync(run, method);
        }

        await RegistrarContract.AssertUnknownDomainIsNotFoundAsync(new ScriptedRegistrarPlugin { HoldsDomain = false }, RegistrarFixtures.DomainContext);
    }

    /// <summary>A read that is refused where the caller arranged a registrar that reads is reported, naming the method.</summary>
    [Fact]
    public async Task AssertReads_Refused_Throws()
    {
        var plugin = new ScriptedRegistrarPlugin
        {
            GetContact = _ => ValueResult<RegistrarContact, RegistrarRefusalReason>.Refused(RegistrarRefusalReason.AccessDenied),
        };

        var run = () => RegistrarContract.AssertReadsAnswerValuesAsync(plugin, RegistrarFixtures.DomainContext);

        await AssertViolationAsync(run, "GetContactAsync", "Refused");
    }

    /// <summary>A plugin that implements only the operations interface is checked without the optional operations, which it does not have.</summary>
    [Fact]
    public async Task AssertUnknownDomain_PluginWithoutOptionalInterfaces_SkipsThem()
    {
        var plugin = new OperationsOnlyRegistrarPlugin(new ScriptedRegistrarPlugin { HoldsDomain = false });

        await RegistrarContract.AssertUnknownDomainIsNotFoundAsync(plugin, RegistrarFixtures.DomainContext);

        plugin.Should().NotBeAssignableTo<IRegistrarEmailForwardingPlugin>().And.NotBeAssignableTo<IRegistrarDnsManagementPlugin>().And.NotBeAssignableTo<IRegistrarWhoisPlugin>();
        plugin.Inner.CallsOf(nameof(IRegistrarOperationsPlugin.AddDnsRecordAsync)).Should().Be(1);
        plugin.Inner.CallsOf(nameof(IRegistrarEmailForwardingPlugin.AddEmailForwardingRuleAsync)).Should().Be(0);
        plugin.Inner.CallsOf(nameof(IRegistrarDnsManagementPlugin.SetDnsManagementAsync)).Should().Be(0);
        plugin.Inner.CallsOf(nameof(IRegistrarWhoisPlugin.GetWhoisAsync)).Should().Be(0);
    }

    /// <summary>Every check of the unknown domain helper rejects a plugin that answers a success for a domain the registrar does not hold, naming the method.</summary>
    [Fact]
    public async Task AssertUnknownDomain_EveryCheck_RejectsASuccess()
    {
        var done = OperationResult<RegistrarRefusalReason>.Done();
        var cases = new (string Method, ScriptedRegistrarPlugin Plugin)[]
        {
            (nameof(IRegistrarOperationsPlugin.GetContactAsync), Unknown(plugin => plugin.GetContact = _ => ValueResult<RegistrarContact, RegistrarRefusalReason>.Done(RegistrarFixtures.Contact))),
            (nameof(IRegistrarOperationsPlugin.CheckDomainActiveAsync), Unknown(plugin => plugin.CheckDomainActive = _ => ValueResult<RegistrarOrder, RegistrarRefusalReason>.Done(new RegistrarOrder("dom-1", null, false)))),
            (nameof(IRegistrarOperationsPlugin.GetEppCodeAsync), Unknown(plugin => plugin.GetEppCode = _ => ValueResult<RegistrarEppCode, RegistrarRefusalReason>.Done(new RegistrarEppCode("code")))),
            (nameof(IRegistrarOperationsPlugin.GetDnsRecordsAsync), Unknown(plugin => plugin.GetDnsRecords = _ => ValueResult<IReadOnlyList<RegistrarDnsRecord>, RegistrarRefusalReason>.Done([]))),
            (nameof(IRegistrarOperationsPlugin.SetContactAsync), Unknown(plugin => plugin.SetContact = _ => done)),
            (nameof(IRegistrarOperationsPlugin.SetAutoRenewAsync), Unknown(plugin => plugin.SetAutoRenew = _ => done)),
            (nameof(IRegistrarOperationsPlugin.SetWhoisPrivacyAsync), Unknown(plugin => plugin.SetWhoisPrivacy = _ => done)),
            (nameof(IRegistrarOperationsPlugin.SetRegistrarLockAsync), Unknown(plugin => plugin.SetRegistrarLock = _ => done)),
            (nameof(IRegistrarOperationsPlugin.CancelTransferAsync), Unknown(plugin => plugin.CancelTransfer = _ => done)),
            (nameof(IRegistrarOperationsPlugin.InitiateOutgoingTransferAsync), Unknown(plugin => plugin.InitiateOutgoingTransfer = _ => done)),
            (nameof(IRegistrarEmailForwardingPlugin.SetEmailForwardingAsync), Unknown(plugin => plugin.SetEmailForwarding = _ => done)),
            (nameof(IRegistrarEmailForwardingPlugin.UpdateEmailForwardingRuleAsync), Unknown(plugin => plugin.UpdateEmailForwardingRule = _ => done)),
            (nameof(IRegistrarEmailForwardingPlugin.DeleteEmailForwardingRuleAsync), Unknown(plugin => plugin.DeleteEmailForwardingRule = _ => done)),
        };

        foreach (var (method, plugin) in cases)
        {
            var run = () => RegistrarContract.AssertUnknownDomainIsNotFoundAsync(plugin, RegistrarFixtures.DomainContext);

            await AssertViolationAsync(run, method, "unknown domain");
        }
    }

    /// <summary>Builds a plugin that holds no domain and applies a script that breaks one promise.</summary>
    /// <param name="script">Sets the one answer that is wrong.</param>
    /// <returns>The plugin.</returns>
    private static ScriptedRegistrarPlugin Unknown(Action<ScriptedRegistrarPlugin> script)
    {
        var plugin = new ScriptedRegistrarPlugin { HoldsDomain = false };
        script(plugin);
        return plugin;
    }
}
