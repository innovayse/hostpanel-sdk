using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Tests.Fakes;
using static Hostpanel.Sdk.Tests.Support.ContractViolationAsserts;

namespace Hostpanel.Sdk.Tests.Provisioning;

/// <summary>
/// Tests of the optional single account read of a hosting panel plugin and of the address an account may carry: the
/// shapes, the capability and the contract check.
/// </summary>
public sealed class PanelAccountReadTests
{
    /// <summary>An account built the old way, with five arguments, binds and carries no address.</summary>
    [Fact]
    public void Account_BuiltTheOldWay_HasNoIpAddress()
    {
        var account = new PanelAccount("acct1", null, PanelAccountStatus.Active, null, null);

        account.IpAddress.Should().BeNull();
    }

    /// <summary>The address is set with an initializer, and it takes part in equality like the other members.</summary>
    [Fact]
    public void Account_WithAnAddress_CarriesItAndComparesByIt()
    {
        var plain = new PanelAccount("acct1", null, PanelAccountStatus.Active, null, null);
        var addressed = plain with { IpAddress = "192.0.2.10" };

        addressed.IpAddress.Should().Be("192.0.2.10");
        addressed.Should().NotBe(plain);
        addressed.Should().Be(new PanelAccount("acct1", null, PanelAccountStatus.Active, null, null) { IpAddress = "192.0.2.10" });
    }

    /// <summary>The capability name the manifest lists is fixed.</summary>
    [Fact]
    public void Capability_Name_IsProvisioningPanelAccount()
    {
        PluginCapabilities.ProvisioningPanelAccount.Should().Be("provisioningPanelAccount");
    }

    /// <summary>
    /// The account read is a new interface that extends the panel interface, which gains no member, so a panel plugin built on
    /// an earlier version compiles and loads as before and is simply not an account reader.
    /// </summary>
    [Fact]
    public void AccountInterface_ExtendsThePanelInterface_AndLeavesItUnchanged()
    {
        typeof(IProvisioningPanelAccountPlugin).GetInterfaces().Should().Contain(typeof(IProvisioningPanelPlugin));
        typeof(IProvisioningPanelAccountPlugin).GetMethods().Select(method => method.Name)
            .Should().Equal(nameof(IProvisioningPanelAccountPlugin.GetAccountAsync));
        typeof(IProvisioningPanelPlugin).GetMethod(nameof(IProvisioningPanelAccountPlugin.GetAccountAsync)).Should().BeNull();
        typeof(IProvisioningPanelPlugin).GetMethods().Should().HaveCount(10);
    }

    /// <summary>A well behaved plugin passes the well formed check and the account comes back to the test.</summary>
    [Fact]
    public async Task AssertAccountIsWellFormed_WellBehavedPlugin_Passes()
    {
        var plugin = new ScriptedPanelPlugin();

        var result = await ProvisioningContract.AssertAccountIsWellFormedAsync(plugin, PanelFixtures.Account);

        result.Outcome.Should().Be(PanelOperationOutcome.Succeeded);
        result.Value.Should().Be(PanelFixtures.ExistingAccount);
        plugin.CallsOf(nameof(IProvisioningPanelAccountPlugin.GetAccountAsync)).Should().Be(1);
    }

    /// <summary>An account that has no address is well formed: a field the panel does not report is null.</summary>
    [Fact]
    public async Task AssertAccountIsWellFormed_WithoutAnAddress_Passes()
    {
        var account = PanelFixtures.ExistingAccount with { IpAddress = null };
        var plugin = new ScriptedPanelPlugin { Account = _ => ValueResult<PanelAccount, PanelRefusalReason>.Done(account) };

        var result = await ProvisioningContract.AssertAccountIsWellFormedAsync(plugin, PanelFixtures.Account);

        result.Value.Should().Be(account);
    }

    /// <summary>A panel that cannot read one account may answer not supported.</summary>
    [Fact]
    public async Task AssertAccountIsWellFormed_NotSupported_Passes()
    {
        var plugin = new ScriptedPanelPlugin { Account = _ => ValueResult<PanelAccount, PanelRefusalReason>.NotSupported() };

        var result = await ProvisioningContract.AssertAccountIsWellFormedAsync(plugin, PanelFixtures.Account);

        result.Outcome.Should().Be(PanelOperationOutcome.NotSupported);
        result.Value.Should().BeNull();
    }

    /// <summary>A plugin that throws, or that answers something other than the account, breaks the promise.</summary>
    [Fact]
    public async Task AssertAccountIsWellFormed_ThrowingOrWrongOutcome_Throws()
    {
        var throwing = new ScriptedPanelPlugin { Account = _ => throw new InvalidOperationException("panel text") };
        var missing = new ScriptedPanelPlugin { Account = _ => ValueResult<PanelAccount, PanelRefusalReason>.NotFound() };
        var noValue = new ScriptedPanelPlugin { Account = _ => new ValueResult<PanelAccount, PanelRefusalReason>(PanelOperationOutcome.Succeeded, null, null) };

        await AssertViolationAsync(() => ProvisioningContract.AssertAccountIsWellFormedAsync(throwing, PanelFixtures.Account), "GetAccountAsync threw on call 1");
        await AssertViolationAsync(() => ProvisioningContract.AssertAccountIsWellFormedAsync(missing, PanelFixtures.Account), "GetAccountAsync answered NotFound");
        await AssertViolationAsync(() => ProvisioningContract.AssertAccountIsWellFormedAsync(noValue, PanelFixtures.Account), "without a value");
    }

    /// <summary>An account with no reference, an undefined status or another reference than the one asked for is refused.</summary>
    /// <param name="reference">The reference of the answered account.</param>
    /// <param name="status">The status of the answered account.</param>
    /// <param name="expected">The text the violation message must contain.</param>
    /// <returns>A task that completes when the check passed.</returns>
    [Theory]
    [InlineData("", PanelAccountStatus.Active, "without a ProvisioningRef")]
    [InlineData(" ", PanelAccountStatus.Active, "without a ProvisioningRef")]
    [InlineData("acct1", (PanelAccountStatus)0, "not a PanelAccountStatus")]
    [InlineData("acct2", PanelAccountStatus.Active, "other than the one it was asked for")]
    public async Task AssertAccountIsWellFormed_MalformedAccount_Throws(string reference, PanelAccountStatus status, string expected)
    {
        var account = new PanelAccount(reference, null, status, null, null);
        var plugin = new ScriptedPanelPlugin { Account = _ => ValueResult<PanelAccount, PanelRefusalReason>.Done(account) };

        await AssertViolationAsync(() => ProvisioningContract.AssertAccountIsWellFormedAsync(plugin, PanelFixtures.Account), "GetAccountAsync", expected);
    }

    /// <summary>An address that is not an IP address is refused, and the message does not repeat it.</summary>
    /// <param name="address">The address of the answered account.</param>
    /// <returns>A task that completes when the check passed.</returns>
    [Theory]
    [InlineData("")]
    [InlineData("not-an-address")]
    [InlineData("panel.example.test")]
    public async Task AssertAccountIsWellFormed_AddressThatIsNotAnIp_Throws(string address)
    {
        var account = PanelFixtures.ExistingAccount with { IpAddress = address };
        var plugin = new ScriptedPanelPlugin { Account = _ => ValueResult<PanelAccount, PanelRefusalReason>.Done(account) };

        var message = await ViolationMessageAsync(() => ProvisioningContract.AssertAccountIsWellFormedAsync(plugin, PanelFixtures.Account));

        message.Should().Contain("GetAccountAsync").And.Contain("IpAddress that is not an IP address");
        message.Should().NotContain("not-an-address").And.NotContain("panel.example.test");
    }

    /// <summary>An IPv6 address is an address too.</summary>
    [Fact]
    public async Task AssertAccountIsWellFormed_Ipv6Address_Passes()
    {
        var account = PanelFixtures.ExistingAccount with { IpAddress = "2001:db8::10" };
        var plugin = new ScriptedPanelPlugin { Account = _ => ValueResult<PanelAccount, PanelRefusalReason>.Done(account) };

        var result = await ProvisioningContract.AssertAccountIsWellFormedAsync(plugin, PanelFixtures.Account);

        result.Value.Should().Be(account);
    }

    /// <summary>A listing is held to the same address rule as a single read, since one piece of code checks both.</summary>
    [Fact]
    public async Task AssertListAccountsIsComplete_AddressThatIsNotAnIp_Throws()
    {
        var account = new PanelAccount("a", null, PanelAccountStatus.Active, null, null) { IpAddress = "not-an-address" };
        var plugin = new ScriptedPanelPlugin { List = _ => PanelAccountListResult.Page([account], null) };

        await AssertViolationAsync(() => ProvisioningContract.AssertListAccountsIsCompleteAsync(plugin, PanelFixtures.Server), "ListAccountsAsync", "IpAddress that is not an IP address on page 1");
    }

    /// <summary>The check refuses a missing plugin or context before it calls anything.</summary>
    /// <returns>A task that completes when the check passed.</returns>
    [Fact]
    public async Task Check_NullArguments_Throw()
    {
        var plugin = new ScriptedPanelPlugin();

        await FluentActions.Awaiting(() => ProvisioningContract.AssertAccountIsWellFormedAsync(null!, PanelFixtures.Account))
            .Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => ProvisioningContract.AssertAccountIsWellFormedAsync(plugin, null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }
}
