using Hostpanel.Sdk.Base;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Registrar;
using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Registrar;

/// <summary>Tests of the shapes of the registrar contract: masking, factories, enum values and the additive guarantee.</summary>
public sealed class RegistrarContractShapeTests
{
    /// <summary>The connection prints its API key masked and everything else as is.</summary>
    [Fact]
    public void Connection_ToString_MasksApiKey()
    {
        var printed = RegistrarFixtures.Connection.ToString();

        printed.Should().NotContain(RegistrarFixtures.ApiKey).And.NotContain("reseller");
        printed.Should().Contain("ApiKey = ***").And.Contain("AccountName = ***").And.Contain("TestMode = False");
    }

    /// <summary>An absent key prints as null, so a log line still shows that none was saved.</summary>
    [Fact]
    public void Connection_ToString_ShowsAbsentKeyAsNull()
    {
        var connection = RegistrarFixtures.Connection with { ApiKey = null };

        connection.ToString().Should().Contain("ApiKey = null");
    }

    /// <summary>An e-mail address in a forwarding rule or in the removal of one prints masked, alone and inside the contexts that hold it.</summary>
    [Fact]
    public void ForwardingAddresses_ToString_AreMasked()
    {
        var rule = RegistrarFixtures.ForwardingRule.Rule;

        var printed = new[] { rule.ToString(), RegistrarFixtures.ForwardingRule.ToString(), RegistrarFixtures.ForwardingRemoval.ToString() };

        foreach (var text in printed)
        {
            text.Should().NotContain("owner@example.test").And.NotContain("info");
        }

        printed[0].Should().Contain("Source = ***").And.Contain("Destination = ***");
        printed[2].Should().Contain("Source = ***").And.Contain("example.test");
        RegistrarFixtures.ForwardingRule.Should().NotBe(RegistrarFixtures.ForwardingRule with { Rule = rule with { Destination = "other@example.test" } });
    }


    /// <summary>The transfer prints the authorization code masked, and the connection inside it masked too.</summary>
    [Fact]
    public void TransferContext_ToString_MasksCodeAndKey()
    {
        var printed = RegistrarFixtures.Transfer.ToString();

        printed.Should().NotContain(RegistrarFixtures.EppCode).And.NotContain(RegistrarFixtures.ApiKey);
        printed.Should().Contain("EppCode = ***").And.Contain("DomainName = example.test");
    }

    /// <summary>Equality still sees the code, so two transfers that differ only in it are not equal.</summary>
    [Fact]
    public void TransferContext_Equality_StillComparesCode()
    {
        var other = RegistrarFixtures.Transfer with { EppCode = "different" };

        other.Should().NotBe(RegistrarFixtures.Transfer);
    }

    /// <summary>The authorization code prints masked, also inside a result that holds it.</summary>
    [Fact]
    public void EppCode_ToString_IsMasked()
    {
        var code = new RegistrarEppCode(RegistrarFixtures.EppCode);

        code.ToString().Should().NotContain(RegistrarFixtures.EppCode).And.Contain("Code = ***");
        ValueResult<RegistrarEppCode, RegistrarRefusalReason>.Done(code).ToString().Should().NotContain(RegistrarFixtures.EppCode);
    }

    /// <summary>The contact prints every personal detail masked, so a context or a result that holds it never leaks it.</summary>
    [Fact]
    public void Contact_ToString_MasksPersonalData()
    {
        var contact = RegistrarFixtures.Contact with { Organization = "Example Organization", Address2 = "Flat 2" };

        var printed = contact.ToString();

        printed.Should().NotContain("Ada").And.NotContain("Lovelace").And.NotContain("ada@example.test").And.NotContain("+1.5550100");
        printed.Should().NotContain("1 Example Street").And.NotContain("Flat 2").And.NotContain("Example Organization").And.NotContain("Example City");
        printed.Should().Contain("Email = ***").And.Contain("Phone = ***").And.Contain("Address1 = ***").And.Contain("Country = US");
        RegistrarFixtures.Contact.ToString().Should().Contain("Organization = null").And.Contain("Address2 = null");
    }

    /// <summary>The contexts and results that hold a contact or a registrant do not leak it either.</summary>
    [Fact]
    public void ContextsAndResults_HoldingPersonalData_DoNotLeakIt()
    {
        var register = RegistrarFixtures.Register.ToString();
        var transfer = RegistrarFixtures.Transfer.ToString();
        var contactChange = RegistrarFixtures.ContactChange.ToString();
        var result = ValueResult<RegistrarContact, RegistrarRefusalReason>.Done(RegistrarFixtures.Contact).ToString();
        var whois = new RegistrarWhois("Example Registrar", "Registrant Name", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch);

        foreach (var printed in new[] { register, transfer, contactChange, result })
        {
            printed.Should().NotContain("ada@example.test").And.NotContain("+1.5550100").And.NotContain("1 Example Street");
        }

        whois.ToString().Should().NotContain("Registrant Name").And.Contain("Registrant = ***").And.Contain("Registrar = Example Registrar");
    }

    /// <summary>The optional interfaces are separate opt ins that extend the operations interface, each with its own capability name.</summary>
    [Fact]
    public void OptionalInterfaces_AreSeparateOptIns_WithOwnCapabilities()
    {
        typeof(IRegistrarEmailForwardingPlugin).GetInterfaces().Should().Contain(typeof(IRegistrarOperationsPlugin));
        typeof(IRegistrarDnsManagementPlugin).GetInterfaces().Should().Contain(typeof(IRegistrarOperationsPlugin));
        typeof(IRegistrarWhoisPlugin).GetInterfaces().Should().Contain(typeof(IRegistrarOperationsPlugin));
        typeof(IRegistrarOperationsPlugin).GetInterfaces().Should().NotContain(typeof(IRegistrarEmailForwardingPlugin));
        typeof(IRegistrarOperationsPlugin).GetMethods().Select(method => method.Name).Should().NotContain(
            ["SetEmailForwardingAsync", "AddEmailForwardingRuleAsync", "SetDnsManagementAsync", "GetWhoisAsync"]);
        PluginCapabilities.RegistrarEmailForwarding.Should().Be("registrarEmailForwarding");
        PluginCapabilities.RegistrarDnsManagement.Should().Be("registrarDnsManagement");
        PluginCapabilities.RegistrarWhois.Should().Be("registrarWhois");
    }

    /// <summary>The capability name the manifest lists is fixed.</summary>
    [Fact]
    public void Capability_Name_IsRegistrarOperations()
    {
        PluginCapabilities.RegistrarOperations.Should().Be("registrarOperations");
    }

    /// <summary>
    /// A registrar plugin built on an earlier version implements only the marker and compiles unchanged; the new interface is
    /// a separate opt in, so the marker and the base class gain no member.
    /// </summary>
    [Fact]
    public void MarkerOnlyPlugin_IsNotAnOperationsPlugin_AndMarkerHasNoMembers()
    {
        IRegistrarPlugin legacy = new MarkerOnlyPlugin();
        IRegistrarPlugin full = new ScriptedRegistrarPlugin();

        legacy.Should().NotBeAssignableTo<IRegistrarOperationsPlugin>();
        full.Should().BeAssignableTo<IRegistrarOperationsPlugin>();
        typeof(IRegistrarPlugin).GetMembers().Should().BeEmpty();
        typeof(IRegistrarOperationsPlugin).GetInterfaces().Should().Contain(typeof(IRegistrarPlugin));
        typeof(RegistrarProviderBase).GetInterfaces().Should().NotContain(typeof(IRegistrarOperationsPlugin));
    }

    /// <summary>A plugin written the old way: it implements the marker and nothing else.</summary>
    private sealed class MarkerOnlyPlugin : IRegistrarPlugin
    {
    }
}
