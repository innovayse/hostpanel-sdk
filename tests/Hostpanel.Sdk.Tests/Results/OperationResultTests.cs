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

    /// <summary>
    /// The older panel results keep the public shape plugins built on earlier versions compiled against: they derive from
    /// object, deconstruct as before, and their only public interface is the equality one a record always has. The internal
    /// interface they gained is not public.
    /// </summary>
    [Fact]
    public void PanelResults_KeepTheirPublicShape()
    {
        var (outcome, reason) = new PanelActionResult(PanelOperationOutcome.Refused, PanelRefusalReason.Other);
        var (createOutcome, reference, _, _, createReason) = PanelCreateResult.Created("acct1");
        var (usageOutcome, usage, usageReason) = PanelUsageResult.NotFound();
        var (ssoOutcome, url, ssoReason) = PanelSsoResult.Issued("https://panel.example.test/login");

        outcome.Should().Be(PanelOperationOutcome.Refused);
        reason.Should().Be(PanelRefusalReason.Other);
        (createOutcome, reference, createReason).Should().Be((PanelOperationOutcome.Succeeded, "acct1", (PanelRefusalReason?)null));
        (usageOutcome, usage, usageReason).Should().Be((PanelOperationOutcome.NotFound, (PanelUsage?)null, (PanelRefusalReason?)null));
        (ssoOutcome, url, ssoReason).Should().Be((PanelOperationOutcome.Succeeded, "https://panel.example.test/login", (PanelRefusalReason?)null));
        PanelActionResult.Done().Should().Be(new PanelActionResult(PanelOperationOutcome.Succeeded, null));

        foreach (var type in new[] { typeof(PanelActionResult), typeof(PanelCreateResult), typeof(PanelUsageResult), typeof(PanelSsoResult) })
        {
            type.BaseType.Should().Be(typeof(object));
            type.GetInterfaces().Where(contract => contract.IsPublic).Should().BeEquivalentTo(new[] { typeof(IEquatable<>).MakeGenericType(type) });
        }
    }
}
