using Hostpanel.Sdk.Plugins.Results;
using Hostpanel.Sdk.Base;
using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Registrar;
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

        printed.Should().NotContain(RegistrarFixtures.ApiKey);
        printed.Should().Contain("ApiKey = ***").And.Contain("AccountName = reseller").And.Contain("TestMode = False");
    }

    /// <summary>An absent key prints as null, so a log line still shows that none was saved.</summary>
    [Fact]
    public void Connection_ToString_ShowsAbsentKeyAsNull()
    {
        var connection = RegistrarFixtures.Connection with { ApiKey = null };

        connection.ToString().Should().Contain("ApiKey = null");
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

    /// <summary>Every action factory sets the outcome and the reason that its name promises.</summary>
    [Fact]
    public void ActionResult_Factories_SetOutcomeAndReason()
    {
        OperationResult<RegistrarRefusalReason>.Done().Should().Be(new OperationResult<RegistrarRefusalReason>(PanelOperationOutcome.Succeeded, null));
        OperationResult<RegistrarRefusalReason>.AlreadyDone().Outcome.Should().Be(PanelOperationOutcome.AlreadyApplied);
        OperationResult<RegistrarRefusalReason>.NotFound().Outcome.Should().Be(PanelOperationOutcome.NotFound);
        OperationResult<RegistrarRefusalReason>.NotSupported().Outcome.Should().Be(PanelOperationOutcome.NotSupported);
        OperationResult<RegistrarRefusalReason>.Refused(RegistrarRefusalReason.ContactInvalid).Should().Be(new OperationResult<RegistrarRefusalReason>(PanelOperationOutcome.Refused, RegistrarRefusalReason.ContactInvalid));
    }

    /// <summary>Every data factory sets the outcome, the value and the reason that its name promises.</summary>
    [Fact]
    public void DataResult_Factories_SetOutcomeValueAndReason()
    {
        var order = new RegistrarOrder("dom-1", null, true);

        ValueResult<RegistrarOrder, RegistrarRefusalReason>.Done(order).Should().Be(new ValueResult<RegistrarOrder, RegistrarRefusalReason>(PanelOperationOutcome.Succeeded, order, null));
        ValueResult<RegistrarOrder, RegistrarRefusalReason>.AlreadyDone(order).Outcome.Should().Be(PanelOperationOutcome.AlreadyApplied);
        ValueResult<RegistrarOrder, RegistrarRefusalReason>.NotFound().Value.Should().BeNull();
        ValueResult<RegistrarOrder, RegistrarRefusalReason>.NotSupported().Outcome.Should().Be(PanelOperationOutcome.NotSupported);
        ValueResult<RegistrarOrder, RegistrarRefusalReason>.Refused(RegistrarRefusalReason.InsufficientFunds).Reason.Should().Be(RegistrarRefusalReason.InsufficientFunds);
    }

    /// <summary>A success cannot be built without its value.</summary>
    [Fact]
    public void DataResult_SuccessWithoutValue_Throws()
    {
        var done = () => ValueResult<RegistrarOrder, RegistrarRefusalReason>.Done(null!);
        var already = () => ValueResult<RegistrarOrder, RegistrarRefusalReason>.AlreadyDone(null!);

        done.Should().Throw<ArgumentNullException>();
        already.Should().Throw<ArgumentNullException>();
    }

    /// <summary>The enums start at 1, so a default-initialised value is not a valid answer, and the values are fixed.</summary>
    [Fact]
    public void Enums_StartAtOne_AndZeroIsUndefined()
    {
        Enum.IsDefined((RegistrarRefusalReason)0).Should().BeFalse();
        Enum.IsDefined((RegistrarDnsRecordType)0).Should().BeFalse();
        ((int)RegistrarRefusalReason.DomainUnavailable).Should().Be(1);
        ((int)RegistrarRefusalReason.Other).Should().Be(10);
        ((int)RegistrarDnsRecordType.A).Should().Be(1);
        ((int)RegistrarDnsRecordType.Srv).Should().Be(7);
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
