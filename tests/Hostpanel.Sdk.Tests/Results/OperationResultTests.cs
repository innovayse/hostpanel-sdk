using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Provisioning;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Tests.Results;

/// <summary>Tests of the shared result types and of the guarantee that the older panel result keeps its public shape.</summary>
public sealed class OperationResultTests
{
    /// <summary>Every action factory sets the outcome and the reason that its name promises, for any reason enum.</summary>
    [Fact]
    public void ActionResult_Factories_SetOutcomeAndReason()
    {
        OperationResult<PanelRefusalReason>.Done().Should().Be(new OperationResult<PanelRefusalReason>(PanelOperationOutcome.Succeeded, null));
        OperationResult<PanelRefusalReason>.AlreadyDone().Outcome.Should().Be(PanelOperationOutcome.AlreadyApplied);
        OperationResult<PanelRefusalReason>.NotFound().Outcome.Should().Be(PanelOperationOutcome.NotFound);
        OperationResult<RegistrarRefusalReason>.NotSupported().Outcome.Should().Be(PanelOperationOutcome.NotSupported);
        OperationResult<RegistrarRefusalReason>.Refused(RegistrarRefusalReason.Other).Reason.Should().Be(RegistrarRefusalReason.Other);
    }

    /// <summary>Every value factory sets the outcome, the value and the reason that its name promises.</summary>
    [Fact]
    public void ValueResult_Factories_SetOutcomeValueAndReason()
    {
        var value = new PanelPackage("basic");

        ValueResult<PanelPackage, PanelRefusalReason>.Done(value).Should().Be(new ValueResult<PanelPackage, PanelRefusalReason>(PanelOperationOutcome.Succeeded, value, null));
        ValueResult<PanelPackage, PanelRefusalReason>.AlreadyDone(value).Outcome.Should().Be(PanelOperationOutcome.AlreadyApplied);
        ValueResult<PanelPackage, PanelRefusalReason>.NotFound().Value.Should().BeNull();
        ValueResult<PanelPackage, PanelRefusalReason>.NotSupported().Outcome.Should().Be(PanelOperationOutcome.NotSupported);
        ValueResult<PanelPackage, PanelRefusalReason>.Refused(PanelRefusalReason.LimitReached).Reason.Should().Be(PanelRefusalReason.LimitReached);
    }

    /// <summary>A success cannot be built without its value.</summary>
    [Fact]
    public void ValueResult_SuccessWithoutValue_Throws()
    {
        var done = () => ValueResult<PanelPackage, PanelRefusalReason>.Done(null!);
        var already = () => ValueResult<PanelPackage, PanelRefusalReason>.AlreadyDone(null!);

        done.Should().Throw<ArgumentNullException>();
        already.Should().Throw<ArgumentNullException>();
    }

    /// <summary>The older panel action result keeps the shape plugins built on earlier versions compiled against.</summary>
    [Fact]
    public void PanelActionResult_KeepsItsPublicShape()
    {
        var result = new PanelActionResult(PanelOperationOutcome.Refused, PanelRefusalReason.Other);

        var (outcome, reason) = result;

        outcome.Should().Be(PanelOperationOutcome.Refused);
        reason.Should().Be(PanelRefusalReason.Other);
        typeof(PanelActionResult).BaseType.Should().Be(typeof(object));
        PanelActionResult.Done().Should().Be(new PanelActionResult(PanelOperationOutcome.Succeeded, null));
    }
}
