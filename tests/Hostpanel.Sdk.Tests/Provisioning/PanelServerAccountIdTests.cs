using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Tests.Support;

namespace Hostpanel.Sdk.Tests.Provisioning;

/// <summary>
/// Tests of the optional account id of <see cref="PanelServerConnection"/> and of the manifest label that turns it on.
/// </summary>
public sealed class PanelServerAccountIdTests
{
    /// <summary>A connection built the old way, with nine arguments, binds and carries no account id.</summary>
    [Fact]
    public void Connection_BuiltTheOldWay_HasNoAccountId()
    {
        var connection = new PanelServerConnection(
            1, "web1", "web1.example", null, null, true, "root", "hash", "token");

        connection.AccountId.Should().BeNull();
    }

    /// <summary>A connection built with the id carries it.</summary>
    [Fact]
    public void Connection_BuiltWithTheId_CarriesIt()
    {
        var connection = new PanelServerConnection(
            1, "web1", "web1.example", null, null, true, "root", "hash", "token", AccountId: "reseller-7");

        connection.AccountId.Should().Be("reseller-7");
        connection.Username.Should().Be("root");
    }

    /// <summary>The printed form shows the id and still masks the two secrets.</summary>
    [Fact]
    public void Connection_ToString_PrintsTheIdAndMasksTheSecrets()
    {
        var connection = new PanelServerConnection(
            1, "web1", "web1.example", null, null, true, "root", "hash-value", "token-value", "reseller-7");

        var printed = connection.ToString();

        printed.Should().Contain("AccountId = reseller-7").And.Contain("Username = root");
        printed.Should().Contain("AccessHash = ***").And.Contain("ApiToken = ***");
        printed.Should().NotContain("hash-value").And.NotContain("token-value");
    }

    /// <summary>A missing secret prints as null, so its absence shows, and a missing plain member prints as the compiler prints a null.</summary>
    [Fact]
    public void Connection_ToString_PrintsMissingSecretsAsNullAndMissingPlainMembersEmpty()
    {
        var connection = new PanelServerConnection(1, "web1", "web1.example", null, null, true, null, null, null);

        connection.ToString().Should().Contain("Username = ,").And.Contain("AccessHash = null").And.EndWith("AccountId =  }");
    }

    /// <summary>The manifest label is off by default and, when set, turns the field on with the plugin's wording.</summary>
    [Fact]
    public void Manifest_ServerAccountIdLabel_IsOptional()
    {
        var plain = PluginManifestFixtures.Minimal(serverAccountIdLabel: null);
        var labelled = PluginManifestFixtures.Minimal(serverAccountIdLabel: "Reseller ID");

        plain.ServerAccountIdLabel.Should().BeNull();
        labelled.ServerAccountIdLabel.Should().Be("Reseller ID");
    }
}
