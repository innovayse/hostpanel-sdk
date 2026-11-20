using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Registrar;
using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Tests.Fakes;
using static Hostpanel.Sdk.Tests.Support.ContractViolationAsserts;

namespace Hostpanel.Sdk.Tests.Registrar;

/// <summary>Tests of the parts of <see cref="RegistrarContract"/> that check the child nameserver, DNSSEC, restore, domain list, suggestion and balance interfaces.</summary>
public sealed class RegistrarNewOptionalContractTests
{
    /// <summary>A creation that is done on the first call and already applied after passes, and counts its calls.</summary>
    [Fact]
    public async Task AssertCreateChildNameserver_RepeatIsAlreadyApplied_Passes()
    {
        var plugin = new ExtendedRegistrarPlugin();

        var first = await RegistrarContract.AssertCreateChildNameserverIsIdempotentAsync(plugin, RegistrarFixtures.ChildNameserver, calls: 4);

        first.Outcome.Should().Be(PanelOperationOutcome.Succeeded);
        plugin.CallsOf(nameof(IRegistrarChildNameserversPlugin.CreateChildNameserverAsync)).Should().Be(4);
    }

    /// <summary>A creation that answers not found on the repeat is rejected, naming the call.</summary>
    [Fact]
    public async Task AssertCreateChildNameserver_NotFoundOnRepeat_Throws()
    {
        var plugin = new ExtendedRegistrarPlugin { CreateChildNameserver = call => call == 1 ? OperationResult<RegistrarRefusalReason>.Done() : OperationResult<RegistrarRefusalReason>.NotFound() };

        var run = () => RegistrarContract.AssertCreateChildNameserverIsIdempotentAsync(plugin, RegistrarFixtures.ChildNameserver);

        await AssertViolationAsync(run, "CreateChildNameserverAsync", "call 2");
    }

    /// <summary>An update that is refused where the caller arranged a host that exists is reported.</summary>
    [Fact]
    public async Task AssertUpdateChildNameserver_Refused_Throws()
    {
        var plugin = new ExtendedRegistrarPlugin { UpdateChildNameserver = _ => OperationResult<RegistrarRefusalReason>.Refused(RegistrarRefusalReason.NameserverInvalid) };

        var run = () => RegistrarContract.AssertUpdateChildNameserverIsIdempotentAsync(plugin, RegistrarFixtures.ChildNameserver);

        await AssertViolationAsync(run, "UpdateChildNameserverAsync", "Refused", "call 1");
    }

    /// <summary>A deletion that answers not found for a host that is already gone is rejected, because the promise is already applied.</summary>
    [Fact]
    public async Task AssertDeleteChildNameserver_NotFoundOnRepeat_Throws()
    {
        var plugin = new ExtendedRegistrarPlugin { DeleteChildNameserver = call => call == 1 ? OperationResult<RegistrarRefusalReason>.Done() : OperationResult<RegistrarRefusalReason>.NotFound() };

        var run = () => RegistrarContract.AssertDeleteChildNameserverIsIdempotentAsync(plugin, RegistrarFixtures.ChildNameserverRemoval);

        await AssertViolationAsync(run, "DeleteChildNameserverAsync", "call 2");
    }

    /// <summary>A deletion that finds the host gone on the repeat and answers already applied passes.</summary>
    [Fact]
    public async Task AssertDeleteChildNameserver_RepeatIsAlreadyApplied_Passes()
    {
        var plugin = new ExtendedRegistrarPlugin();

        var first = await RegistrarContract.AssertDeleteChildNameserverIsIdempotentAsync(plugin, RegistrarFixtures.ChildNameserverRemoval);

        first.Outcome.Should().Be(PanelOperationOutcome.Succeeded);
    }

    /// <summary>A DNSSEC change that is done and then already applied passes, and one that throws on the repeat is rejected with the exception type only.</summary>
    [Fact]
    public async Task AssertSetDnssec_RepeatBehaviour_IsChecked()
    {
        var good = new ExtendedRegistrarPlugin();
        var throwing = new ExtendedRegistrarPlugin
        {
            SetDnssec = call => call == 1 ? OperationResult<RegistrarRefusalReason>.Done() : throw new InvalidOperationException("key 12345 not found"),
        };

        await RegistrarContract.AssertSetDnssecIsIdempotentAsync(good, RegistrarFixtures.Dnssec);
        var message = await ViolationMessageAsync(() => RegistrarContract.AssertSetDnssecIsIdempotentAsync(throwing, RegistrarFixtures.Dnssec));

        good.CallsOf(nameof(IRegistrarDnssecPlugin.SetDnssecAsync)).Should().Be(3);
        message.Should().Contain("InvalidOperationException").And.NotContain("12345");
    }

    /// <summary>A restore that finds the earlier one and answers already applied with the same reference passes.</summary>
    [Fact]
    public async Task AssertRestore_RepeatIsAlreadyApplied_Passes()
    {
        var plugin = new ExtendedRegistrarPlugin();

        var first = await RegistrarContract.AssertRestoreIsIdempotentAsync(plugin, RegistrarFixtures.DomainContext);

        first.Outcome.Should().Be(PanelOperationOutcome.Succeeded);
        plugin.CallsOf(nameof(IRegistrarRestorePlugin.RestoreDomainAsync)).Should().Be(3);
    }

    /// <summary>A restore that restores again on the repeat, answering a success instead of already applied, is rejected.</summary>
    [Fact]
    public async Task AssertRestore_RestoresTwice_Throws()
    {
        var plugin = new ExtendedRegistrarPlugin
        {
            RestoreDomain = _ => ValueResult<RegistrarOrder, RegistrarRefusalReason>.Done(new RegistrarOrder("dom-1", DateTimeOffset.UnixEpoch, false)),
        };

        var run = () => RegistrarContract.AssertRestoreIsIdempotentAsync(plugin, RegistrarFixtures.DomainContext);

        await AssertViolationAsync(run, "call 2");
    }

    /// <summary>The optional reads pass for a plugin that implements them, and each one is called once.</summary>
    [Fact]
    public async Task AssertOptionalReads_AnswerValues_Passes()
    {
        var plugin = new ExtendedRegistrarPlugin();

        await RegistrarContract.AssertOptionalReadsAnswerValuesAsync(plugin, RegistrarFixtures.DomainContext);

        plugin.CallsOf(nameof(IRegistrarDnssecPlugin.GetDnssecAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IRegistrarDomainListPlugin.ListDomainsAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IRegistrarSuggestionsPlugin.GetSuggestionsAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IRegistrarBalancePlugin.GetBalanceAsync)).Should().Be(1);
    }

    /// <summary>A plugin that implements none of the new interfaces is checked without them, and a read that is refused is reported, naming the method.</summary>
    [Fact]
    public async Task AssertOptionalReads_SkipsMissingInterfaces_AndReportsRefusal()
    {
        var legacy = new ScriptedRegistrarPlugin();
        var refusing = new ExtendedRegistrarPlugin { GetBalance = _ => ValueResult<RegistrarBalance, RegistrarRefusalReason>.Refused(RegistrarRefusalReason.AccessDenied) };

        await RegistrarContract.AssertOptionalReadsAnswerValuesAsync(legacy, RegistrarFixtures.DomainContext);
        var run = () => RegistrarContract.AssertOptionalReadsAnswerValuesAsync(refusing, RegistrarFixtures.DomainContext);

        await AssertViolationAsync(run, "GetBalanceAsync", "Refused");
    }

    /// <summary>A plugin that answers not found on every new operation for a domain it does not hold passes, and each operation is called once.</summary>
    [Fact]
    public async Task AssertUnknownDomain_NewOperations_PassWhenNotFound()
    {
        var plugin = new ExtendedRegistrarPlugin { HoldsDomain = false };

        await RegistrarContract.AssertUnknownDomainIsNotFoundAsync(plugin, RegistrarFixtures.DomainContext);

        plugin.CallsOf(nameof(IRegistrarChildNameserversPlugin.CreateChildNameserverAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IRegistrarChildNameserversPlugin.UpdateChildNameserverAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IRegistrarChildNameserversPlugin.DeleteChildNameserverAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IRegistrarDnssecPlugin.GetDnssecAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IRegistrarDnssecPlugin.SetDnssecAsync)).Should().Be(1);
        plugin.CallsOf(nameof(IRegistrarRestorePlugin.RestoreDomainAsync)).Should().Be(1);
    }

    /// <summary>The unknown domain check covers the new operations: a plugin that changes or reads on a domain it does not hold is rejected, naming the method.</summary>
    [Fact]
    public async Task AssertUnknownDomain_NewOperations_AreCovered()
    {
        var done = OperationResult<RegistrarRefusalReason>.Done();
        var runs = new (string Method, ExtendedRegistrarPlugin Plugin)[]
        {
            (nameof(IRegistrarChildNameserversPlugin.CreateChildNameserverAsync), new ExtendedRegistrarPlugin { HoldsDomain = false, CreateChildNameserver = _ => done }),
            (nameof(IRegistrarChildNameserversPlugin.UpdateChildNameserverAsync), new ExtendedRegistrarPlugin { HoldsDomain = false, UpdateChildNameserver = _ => done }),
            (nameof(IRegistrarChildNameserversPlugin.DeleteChildNameserverAsync), new ExtendedRegistrarPlugin { HoldsDomain = false, DeleteChildNameserver = _ => done }),
            (nameof(IRegistrarDnssecPlugin.SetDnssecAsync), new ExtendedRegistrarPlugin { HoldsDomain = false, SetDnssec = _ => done }),
            (
                nameof(IRegistrarDnssecPlugin.GetDnssecAsync),
                new ExtendedRegistrarPlugin { HoldsDomain = false, GetDnssec = _ => ValueResult<IReadOnlyList<RegistrarDsRecord>, RegistrarRefusalReason>.Done([]) }),
            (
                nameof(IRegistrarRestorePlugin.RestoreDomainAsync),
                new ExtendedRegistrarPlugin { HoldsDomain = false, RestoreDomain = _ => ValueResult<RegistrarOrder, RegistrarRefusalReason>.Done(new RegistrarOrder("dom-1", null, false)) }),
        };

        foreach (var (method, plugin) in runs)
        {
            var run = () => RegistrarContract.AssertUnknownDomainIsNotFoundAsync(plugin, RegistrarFixtures.DomainContext);

            await AssertViolationAsync(run, method);
        }
    }
}
