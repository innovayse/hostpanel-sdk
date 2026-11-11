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
        RegistrarDataResult<RegistrarEppCode>.Done(code).ToString().Should().NotContain(RegistrarFixtures.EppCode);
    }

    /// <summary>Every action factory sets the outcome and the reason that its name promises.</summary>
    [Fact]
    public void ActionResult_Factories_SetOutcomeAndReason()
    {
        RegistrarActionResult.Done().Should().Be(new RegistrarActionResult(PanelOperationOutcome.Succeeded, null));
        RegistrarActionResult.AlreadyDone().Outcome.Should().Be(PanelOperationOutcome.AlreadyApplied);
        RegistrarActionResult.NotFound().Outcome.Should().Be(PanelOperationOutcome.NotFound);
        RegistrarActionResult.NotSupported().Outcome.Should().Be(PanelOperationOutcome.NotSupported);
        RegistrarActionResult.Refused(RegistrarRefusalReason.ContactInvalid).Should().Be(new RegistrarActionResult(PanelOperationOutcome.Refused, RegistrarRefusalReason.ContactInvalid));
    }

    /// <summary>Every data factory sets the outcome, the value and the reason that its name promises.</summary>
    [Fact]
    public void DataResult_Factories_SetOutcomeValueAndReason()
    {
        var order = new RegistrarOrder("dom-1", null, true);

        RegistrarDataResult<RegistrarOrder>.Done(order).Should().Be(new RegistrarDataResult<RegistrarOrder>(PanelOperationOutcome.Succeeded, order, null));
        RegistrarDataResult<RegistrarOrder>.AlreadyDone(order).Outcome.Should().Be(PanelOperationOutcome.AlreadyApplied);
        RegistrarDataResult<RegistrarOrder>.NotFound().Value.Should().BeNull();
        RegistrarDataResult<RegistrarOrder>.NotSupported().Outcome.Should().Be(PanelOperationOutcome.NotSupported);
        RegistrarDataResult<RegistrarOrder>.Refused(RegistrarRefusalReason.InsufficientFunds).Reason.Should().Be(RegistrarRefusalReason.InsufficientFunds);
    }

    /// <summary>A success cannot be built without its value.</summary>
    [Fact]
    public void DataResult_SuccessWithoutValue_Throws()
    {
        var done = () => RegistrarDataResult<RegistrarOrder>.Done(null!);
        var already = () => RegistrarDataResult<RegistrarOrder>.AlreadyDone(null!);

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
