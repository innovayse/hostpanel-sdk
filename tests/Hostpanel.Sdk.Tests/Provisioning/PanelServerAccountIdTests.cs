using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Tests.Provisioning;

/// <summary>Tests of the optional account id of <see cref="PanelServerConnection"/> and of the manifest label that turns it on.</summary>
public sealed class PanelServerAccountIdTests
{
    /// <summary>A connection built the old way, with nine arguments, binds and carries no account id.</summary>
    [Fact]
    public void Connection_BuiltTheOldWay_HasNoAccountId()
    {
        var connection = new PanelServerConnection(1, "web1", "web1.example", null, null, true, "root", "hash", "token");

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

        printed.Should().Contain("AccountId = reseller-7").And.Contain("AccessHash = ***").And.Contain("ApiToken = ***");
        printed.Should().NotContain("hash-value").And.NotContain("token-value");
    }

    /// <summary>A missing id prints as null.</summary>
    [Fact]
    public void Connection_ToString_PrintsMissingIdAsNull()
    {
        var connection = new PanelServerConnection(1, "web1", "web1.example", null, null, true, null, null, null);

        connection.ToString().Should().Contain("AccountId = null");
    }

    /// <summary>The manifest label is off by default and, when set, turns the field on with the plugin's own wording.</summary>
    [Fact]
    public void Manifest_ServerAccountIdLabel_IsOptional()
    {
        var plain = Build(null);
        var labelled = Build("Reseller ID");

        plain.ServerAccountIdLabel.Should().BeNull();
        labelled.ServerAccountIdLabel.Should().Be("Reseller ID");
    }

    /// <summary>Builds a minimal provisioning manifest.</summary>
    /// <param name="label">The account id label, or <see langword="null"/> for none.</param>
    /// <returns>The manifest.</returns>
    private static PluginManifest Build(string? label)
    {
        return new PluginManifest
        {
            Id = "demo",
            Name = "Demo",
            Version = "1.0.0",
            Author = "A",
            Description = "D",
            Type = PluginType.Provisioning,
            Category = "Hosting",
            EntryPoint = "Demo.Plugin",
            SdkVersion = "3.7.0",
            ServerAccountIdLabel = label,
        };
    }
}
