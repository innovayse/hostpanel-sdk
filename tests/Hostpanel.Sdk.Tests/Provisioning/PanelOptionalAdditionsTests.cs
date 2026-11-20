using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Tests.Fakes;
using static Hostpanel.Sdk.Tests.Support.ContractViolationAsserts;

namespace Hostpanel.Sdk.Tests.Provisioning;

/// <summary>
/// Tests of the optional additions to the panel contract: richer usage, a target for single sign on, the account level, the
/// product options, the pending outcome and the package listing. Each is additive, so the older shapes still build.
/// </summary>
public sealed class PanelOptionalAdditionsTests
{
    /// <summary>The usage built the old way has none of the new figures, and an object initializer sets them without a new constructor.</summary>
    [Fact]
    public void Usage_NewFigures_AreOptionalInitMembers()
    {
        var old = new PanelUsage(1, 2, 3, 4, DateTimeOffset.UnixEpoch);
        var rich = old with { InodesUsed = 10, InodesLimit = 20, MailboxesUsed = 1, MailboxesLimit = 5, DatabasesUsed = 2, DatabasesLimit = 6, DomainsUsed = 3, DomainsLimit = 7 };

        old.InodesUsed.Should().BeNull();
        old.DomainsLimit.Should().BeNull();
        rich.InodesLimit.Should().Be(20);
        rich.MailboxesUsed.Should().Be(1);
        rich.DatabasesLimit.Should().Be(6);
        rich.DomainsUsed.Should().Be(3);
        typeof(PanelUsage).GetConstructors().Should().ContainSingle().Which.GetParameters().Should().HaveCount(5);
    }

    /// <summary>The single sign on target and the account level are null by default, so a caller built on an earlier version sends neither.</summary>
    [Fact]
    public void Contexts_NewMembers_AreNullByDefault()
    {
        new PanelSsoContext(PanelFixtures.Server, PanelFixtures.Service, null).TargetPath.Should().BeNull();
        PanelFixtures.Create.Level.Should().BeNull();
        new PanelListAccountsContext(PanelFixtures.Server, null).Level.Should().BeNull();
        PanelFixtures.ChangePackage.ProductOptions.Should().BeNull();
    }

    /// <summary>The new members are set with <c>with</c> and the constructors keep their old shape.</summary>
    [Fact]
    public void Contexts_NewMembers_AreSetWithAnInitializer()
    {
        var sso = new PanelSsoContext(PanelFixtures.Server, PanelFixtures.Service, "192.0.2.1") { TargetPath = "/filemanager" };
        var create = PanelFixtures.Create with { Level = PanelAccountLevel.Reseller };
        var list = new PanelListAccountsContext(PanelFixtures.Server, null) { Level = PanelAccountLevel.Reseller };
        var change = PanelFixtures.ChangePackage with { ProductOptions = new Dictionary<string, string> { ["region"] = "eu" } };

        sso.TargetPath.Should().Be("/filemanager");
        create.Level.Should().Be(PanelAccountLevel.Reseller);
        list.Level.Should().Be(PanelAccountLevel.Reseller);
        change.ProductOptions.Should().ContainKey("region").WhoseValue.Should().Be("eu");
        typeof(PanelSsoContext).GetConstructors().Should().ContainSingle().Which.GetParameters().Should().HaveCount(3);
        typeof(PanelCreateAccountContext).GetConstructors().Should().ContainSingle().Which.GetParameters().Should().HaveCount(7);
    }

    /// <summary>The create context still prints its password masked after it gained a member.</summary>
    [Fact]
    public void CreateContext_WithLevel_StillMasksThePassword()
    {
        var printed = (PanelFixtures.Create with { Level = PanelAccountLevel.Reseller }).ToString();

        printed.Should().NotContain(PanelFixtures.Password).And.Contain("Password = ***").And.Contain("Level = Reseller");
    }

    /// <summary>The pending factories set the outcome, keep the reference of a create and carry no reason.</summary>
    [Fact]
    public void PendingFactories_SetTheOutcome()
    {
        PanelActionResult.Pending().Should().Be(new PanelActionResult(PanelOperationOutcome.Pending, null));
        PanelCreateResult.Pending("acct1").Should().Be(new PanelCreateResult(PanelOperationOutcome.Pending, "acct1", null, null, null));
        ((int)PanelOperationOutcome.Pending).Should().Be(6);
        Enum.IsDefined(PanelOperationOutcome.Pending).Should().BeTrue();
    }

    /// <summary>An action that the panel finishes in the background and answers pending, then done, passes the idempotency check.</summary>
    [Fact]
    public async Task AssertIdempotent_PendingThenAlreadyDone_Passes()
    {
        var plugin = new ScriptedPanelPlugin
        {
            Suspend = call => call == 1 ? PanelActionResult.Pending() : PanelActionResult.AlreadyDone(),
        };

        var first = await ProvisioningContract.AssertSuspendIsIdempotentAsync(plugin, PanelFixtures.Suspend);

        first.Outcome.Should().Be(PanelOperationOutcome.Pending);
    }

    /// <summary>A create that answers pending while it runs and already applied once it is done passes, and the reference stays the same.</summary>
    [Fact]
    public async Task AssertCreate_PendingThenAlreadyExists_Passes()
    {
        var plugin = new ScriptedPanelPlugin
        {
            Create = call => call switch
            {
                1 => PanelCreateResult.Pending("acct1"),
                2 => PanelCreateResult.Pending("acct1"),
                _ => PanelCreateResult.AlreadyExists("acct1", "example.test"),
            },
        };

        var first = await ProvisioningContract.AssertCreateIsIdempotentAsync(plugin, PanelFixtures.Create, calls: 4);

        first.Outcome.Should().Be(PanelOperationOutcome.Pending);
        first.ProvisioningRef.Should().Be("acct1");
    }

    /// <summary>A create that was done and then goes back to pending is rejected: a pending answer only continues a pending one.</summary>
    [Fact]
    public async Task AssertCreate_PendingAfterDone_Throws()
    {
        var plugin = new ScriptedPanelPlugin
        {
            Create = call => call == 1 ? PanelCreateResult.Created("acct1") : PanelCreateResult.Pending("acct1"),
        };

        var run = () => ProvisioningContract.AssertCreateIsIdempotentAsync(plugin, PanelFixtures.Create);

        await AssertViolationAsync(run, "answered Pending on call 2");
    }

    /// <summary>A pending answer that carries a refusal reason is rejected like any other outcome with a stray reason.</summary>
    [Fact]
    public async Task AssertIdempotent_PendingWithAReason_Throws()
    {
        var plugin = new ScriptedPanelPlugin
        {
            Suspend = _ => new PanelActionResult(PanelOperationOutcome.Pending, PanelRefusalReason.Other),
        };

        var run = () => ProvisioningContract.AssertSuspendIsIdempotentAsync(plugin, PanelFixtures.Suspend);

        await AssertViolationAsync(run, "only a refusal has one");
    }

    /// <summary>The package listing is an interface that extends the panel interface, which gains no member, with its own capability.</summary>
    [Fact]
    public void PackagesInterface_ExtendsThePanelInterface_AndHasItsCapability()
    {
        typeof(IProvisioningPanelPackagesPlugin).GetInterfaces().Should().Contain(typeof(IProvisioningPanelPlugin));
        typeof(IProvisioningPanelPackagesPlugin).GetMethods().Select(method => method.Name)
            .Should().Equal(nameof(IProvisioningPanelPackagesPlugin.ListPackagesAsync));
        typeof(IProvisioningPanelPlugin).GetMethod(nameof(IProvisioningPanelPackagesPlugin.ListPackagesAsync)).Should().BeNull();
        PluginCapabilities.ProvisioningPanelPackages.Should().Be("provisioningPanelPackages");
    }

    /// <summary>A listing of packages with limits passes and is returned to the test.</summary>
    [Fact]
    public async Task AssertPackages_WellFormed_Passes()
    {
        var plugin = new ScriptedPanelPlugin();

        var packages = await ProvisioningContract.AssertPackagesAreWellFormedAsync(plugin, PanelFixtures.Server);

        packages.Should().ContainSingle().Which.Name.Should().Be("basic");
        plugin.CallsOf(nameof(IProvisioningPanelPackagesPlugin.ListPackagesAsync)).Should().Be(1);
    }

    /// <summary>A panel that has no package listing answers not supported, and the helper accepts that.</summary>
    [Fact]
    public async Task AssertPackages_NotSupported_PassesWithNoPackages()
    {
        var plugin = new ScriptedPanelPlugin
        {
            Packages = _ => ValueResult<IReadOnlyList<PanelPackageDetails>, PanelRefusalReason>.NotSupported(),
        };

        var packages = await ProvisioningContract.AssertPackagesAreWellFormedAsync(plugin, PanelFixtures.Server);

        packages.Should().BeEmpty();
    }

    /// <summary>A package without a name, a repeated name and a negative limit are each rejected.</summary>
    [Fact]
    public async Task AssertPackages_Malformed_Throws()
    {
        var unnamed = new ScriptedPanelPlugin { Packages = _ => Listing(new PanelPackageDetails(" ", null, null, null, null, null, null)) };
        var twice = new ScriptedPanelPlugin
        {
            Packages = _ => Listing(new PanelPackageDetails("a", null, null, null, null, null, null), new PanelPackageDetails("a", null, null, null, null, null, null)),
        };
        var negative = new ScriptedPanelPlugin { Packages = _ => Listing(new PanelPackageDetails("a", null, null, null, -1, null, null)) };

        await AssertViolationAsync(() => ProvisioningContract.AssertPackagesAreWellFormedAsync(unnamed, PanelFixtures.Server), "without a Name");
        await AssertViolationAsync(() => ProvisioningContract.AssertPackagesAreWellFormedAsync(twice, PanelFixtures.Server), "name twice");
        await AssertViolationAsync(() => ProvisioningContract.AssertPackagesAreWellFormedAsync(negative, PanelFixtures.Server), "negative limit");
    }

    /// <summary>A package listing the panel refuses is rejected: the caller arranges a panel that lists, and a refusal is not a list.</summary>
    [Fact]
    public async Task AssertPackages_RefusedOrNotFound_Throws()
    {
        var plugin = new ScriptedPanelPlugin
        {
            Packages = _ => ValueResult<IReadOnlyList<PanelPackageDetails>, PanelRefusalReason>.Refused(PanelRefusalReason.AccessDenied),
        };

        var run = () => ProvisioningContract.AssertPackagesAreWellFormedAsync(plugin, PanelFixtures.Server);

        await AssertViolationAsync(run, "ListPackagesAsync answered Refused");
    }

    /// <summary>Builds a successful package listing.</summary>
    /// <param name="packages">The packages the listing answers.</param>
    /// <returns>The answer.</returns>
    private static ValueResult<IReadOnlyList<PanelPackageDetails>, PanelRefusalReason> Listing(params PanelPackageDetails[] packages)
    {
        return ValueResult<IReadOnlyList<PanelPackageDetails>, PanelRefusalReason>.Done(packages);
    }
}
