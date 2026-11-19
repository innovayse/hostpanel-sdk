using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Tests.Fakes;
using static Hostpanel.Sdk.Tests.Support.ContractViolationAsserts;

namespace Hostpanel.Sdk.Tests.Provisioning;

/// <summary>
/// Tests of the optional password of <see cref="PanelServerConnection"/> and of the login details capability of a hosting
/// panel plugin: the shapes, the masking and the contract checks.
/// </summary>
public sealed class PanelLoginDetailsTests
{
    /// <summary>A connection built the old way, with ten arguments, binds and carries no password.</summary>
    [Fact]
    public void Connection_BuiltTheOldWay_HasNoPassword()
    {
        var connection = new PanelServerConnection(
            1, "web1", "web1.example", null, null, true, "root", "hash", "token", "reseller-7");

        connection.Password.Should().BeNull();
        connection.AccountId.Should().Be("reseller-7");
    }

    /// <summary>A connection built with a password carries it and keeps the other members.</summary>
    [Fact]
    public void Connection_BuiltWithAPassword_CarriesIt()
    {
        PanelFixtures.PasswordServer.Password.Should().Be(PanelFixtures.ServerPassword);
        PanelFixtures.PasswordServer.AccessHash.Should().BeNull();
        PanelFixtures.PasswordServer.ApiToken.Should().BeNull();
        PanelFixtures.PasswordServer.Username.Should().Be("root");
    }

    /// <summary>The password never appears in the printed form of a connection.</summary>
    [Fact]
    public void Connection_ToString_MasksThePassword()
    {
        var printed = PanelFixtures.PasswordServer.ToString();

        printed.Should().Contain("Password = ***").And.Contain("Username = root");
        printed.Should().NotContain(PanelFixtures.ServerPassword);
        ProvisioningContract.AssertHidesSecrets(PanelFixtures.PasswordServer, PanelFixtures.ServerPassword);
    }

    /// <summary>A missing password prints as null, so its absence shows, and the other secrets stay masked.</summary>
    [Fact]
    public void Connection_WithoutAPassword_PrintsNullAndKeepsTheOtherSecretsMasked()
    {
        var printed = PanelFixtures.Server.ToString();

        printed.Should().Contain("Password = null").And.Contain("AccessHash = ***").And.Contain("ApiToken = ***");
        printed.Should().NotContain(PanelFixtures.AccessHash).And.NotContain(PanelFixtures.ApiToken);
    }

    /// <summary>The capability name the manifest lists is fixed.</summary>
    [Fact]
    public void Capability_Name_IsProvisioningPanelLogin()
    {
        PluginCapabilities.ProvisioningPanelLogin.Should().Be("provisioningPanelLogin");
    }

    /// <summary>
    /// The login capability is a new interface that extends the panel interface, which gains no member, so a panel plugin built
    /// on an earlier version compiles and loads as before and is simply not a login plugin.
    /// </summary>
    [Fact]
    public void LoginInterface_ExtendsThePanelInterface_AndLeavesItUnchanged()
    {
        typeof(IProvisioningPanelLoginPlugin).GetInterfaces().Should().Contain(typeof(IProvisioningPanelPlugin));
        typeof(IProvisioningPanelLoginPlugin).GetMethods().Select(method => method.Name)
            .Should().Equal(nameof(IProvisioningPanelLoginPlugin.GetLoginDetailsAsync));
        typeof(IProvisioningPanelPlugin).GetMethod(nameof(IProvisioningPanelLoginPlugin.GetLoginDetailsAsync)).Should().BeNull();
        typeof(IProvisioningPanelPlugin).GetMethods().Should().HaveCount(10);
    }

    /// <summary>The details print without the password and keep what is not secret.</summary>
    [Fact]
    public void Details_ToString_MasksThePassword()
    {
        var printed = PanelFixtures.LoginDetails.ToString();

        printed.Should().Contain("Password = ***").And.Contain("Username = acct1").And.Contain("LoginUrl = https://panel.example.test:2083/");
        printed.Should().NotContain(PanelFixtures.LoginPassword);
    }

    /// <summary>Details without a password print it as null, the usual case for a panel that never reveals one.</summary>
    [Fact]
    public void Details_WithoutAPassword_PrintsNull()
    {
        var details = new PanelLoginDetails("acct1", null, null, null, "https://panel.example.test/");

        details.ToString().Should().Contain("Password = null");
    }

    /// <summary>A result that carries details prints them masked too, so a log line of the result never shows the password.</summary>
    [Fact]
    public void Result_ToString_MasksThePassword()
    {
        var result = ValueResult<PanelLoginDetails, PanelRefusalReason>.Done(PanelFixtures.LoginDetails);

        result.ToString().Should().NotContain(PanelFixtures.LoginPassword);
        ProvisioningContract.AssertHidesSecrets(result, PanelFixtures.LoginPassword);
    }

    /// <summary>A well behaved plugin passes the readable check and the details come back to the test.</summary>
    [Fact]
    public async Task AssertLoginDetailsAreReadable_WellBehavedPlugin_Passes()
    {
        var plugin = new ScriptedPanelPlugin();

        var result = await ProvisioningContract.AssertLoginDetailsAreReadableAsync(plugin, PanelFixtures.Account);

        result.Outcome.Should().Be(PanelOperationOutcome.Succeeded);
        result.Value.Should().Be(PanelFixtures.LoginDetails);
        plugin.CallsOf("GetLoginDetailsAsync").Should().Be(1);
    }

    /// <summary>A panel that has no sign-in page to show may answer not supported.</summary>
    [Fact]
    public async Task AssertLoginDetailsAreReadable_NotSupported_Passes()
    {
        var plugin = new ScriptedPanelPlugin { Login = _ => ValueResult<PanelLoginDetails, PanelRefusalReason>.NotSupported() };

        var result = await ProvisioningContract.AssertLoginDetailsAreReadableAsync(plugin, PanelFixtures.Account);

        result.Outcome.Should().Be(PanelOperationOutcome.NotSupported);
        result.Value.Should().BeNull();
    }

    /// <summary>A plugin that throws, or that answers something other than the details, breaks the promise.</summary>
    [Fact]
    public async Task AssertLoginDetailsAreReadable_ThrowingOrWrongOutcome_Throws()
    {
        var throwing = new ScriptedPanelPlugin { Login = _ => throw new InvalidOperationException("panel text") };
        var missing = new ScriptedPanelPlugin { Login = _ => ValueResult<PanelLoginDetails, PanelRefusalReason>.NotFound() };
        var noValue = new ScriptedPanelPlugin { Login = _ => new ValueResult<PanelLoginDetails, PanelRefusalReason>(PanelOperationOutcome.Succeeded, null, null) };

        await AssertViolationAsync(() => ProvisioningContract.AssertLoginDetailsAreReadableAsync(throwing, PanelFixtures.Account), "GetLoginDetailsAsync threw on call 1");
        await AssertViolationAsync(() => ProvisioningContract.AssertLoginDetailsAreReadableAsync(missing, PanelFixtures.Account), "GetLoginDetailsAsync answered NotFound");
        await AssertViolationAsync(() => ProvisioningContract.AssertLoginDetailsAreReadableAsync(noValue, PanelFixtures.Account), "without a value");
    }

    /// <summary>Details with no user name, or with an address that is not an absolute web address, are refused.</summary>
    /// <param name="username">The user name of the details.</param>
    /// <param name="loginUrl">The sign-in address of the details.</param>
    /// <param name="expected">The text the violation message must contain.</param>
    /// <returns>A task that completes when the check passed.</returns>
    [Theory]
    [InlineData("", "https://panel.example.test/", "without a Username")]
    [InlineData(" ", "https://panel.example.test/", "without a Username")]
    [InlineData("acct1", "/login", "not an absolute http or https address")]
    [InlineData("acct1", "ftp://panel.example.test/", "not an absolute http or https address")]
    [InlineData("acct1", "", "not an absolute http or https address")]
    public async Task AssertLoginDetailsAreReadable_MalformedDetails_Throw(string username, string loginUrl, string expected)
    {
        var details = new PanelLoginDetails(username, null, null, null, loginUrl);
        var plugin = new ScriptedPanelPlugin { Login = _ => ValueResult<PanelLoginDetails, PanelRefusalReason>.Done(details) };

        await AssertViolationAsync(() => ProvisioningContract.AssertLoginDetailsAreReadableAsync(plugin, PanelFixtures.Account), "GetLoginDetailsAsync", expected);
    }

    /// <summary>The message of a malformed address never repeats the address the plugin answered.</summary>
    [Fact]
    public async Task AssertLoginDetailsAreReadable_Message_DoesNotRepeatTheAddress()
    {
        var details = new PanelLoginDetails("acct1", null, null, null, "/secret-path?token=abc");
        var plugin = new ScriptedPanelPlugin { Login = _ => ValueResult<PanelLoginDetails, PanelRefusalReason>.Done(details) };

        var message = await ViolationMessageAsync(() => ProvisioningContract.AssertLoginDetailsAreReadableAsync(plugin, PanelFixtures.Account));

        message.Should().NotContain("secret-path").And.NotContain("abc");
    }

    /// <summary>An unknown account may be answered not found or not supported.</summary>
    [Fact]
    public async Task AssertUnknownAccountLoginIsNotFound_NotFoundAndNotSupported_Pass()
    {
        var missing = new ScriptedPanelPlugin { Login = _ => ValueResult<PanelLoginDetails, PanelRefusalReason>.NotFound() };
        var unsupported = new ScriptedPanelPlugin { Login = _ => ValueResult<PanelLoginDetails, PanelRefusalReason>.NotSupported() };

        await ProvisioningContract.AssertUnknownAccountLoginIsNotFoundAsync(missing, PanelFixtures.Account);
        await ProvisioningContract.AssertUnknownAccountLoginIsNotFoundAsync(unsupported, PanelFixtures.Account);
    }

    /// <summary>A plugin that answers details for an unknown account, or throws, breaks the promise.</summary>
    [Fact]
    public async Task AssertUnknownAccountLoginIsNotFound_DetailsOrThrow_Throws()
    {
        var invented = new ScriptedPanelPlugin();
        var throwing = new ScriptedPanelPlugin { Login = _ => throw new InvalidOperationException("panel text") };

        await AssertViolationAsync(() => ProvisioningContract.AssertUnknownAccountLoginIsNotFoundAsync(invented, PanelFixtures.Account), "GetLoginDetailsAsync answered Succeeded for an unknown account");
        await AssertViolationAsync(() => ProvisioningContract.AssertUnknownAccountLoginIsNotFoundAsync(throwing, PanelFixtures.Account), "GetLoginDetailsAsync threw on call 1");
    }

    /// <summary>The checks refuse a missing plugin or context before they call anything.</summary>
    /// <returns>A task that completes when the check passed.</returns>
    [Fact]
    public async Task Checks_NullArguments_Throw()
    {
        var plugin = new ScriptedPanelPlugin();

        await FluentActions.Awaiting(() => ProvisioningContract.AssertLoginDetailsAreReadableAsync(null!, PanelFixtures.Account))
            .Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => ProvisioningContract.AssertLoginDetailsAreReadableAsync(plugin, null!))
            .Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => ProvisioningContract.AssertUnknownAccountLoginIsNotFoundAsync(null!, PanelFixtures.Account))
            .Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => ProvisioningContract.AssertUnknownAccountLoginIsNotFoundAsync(plugin, null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }
}
