using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Tax;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Tax;

/// <summary>
/// Tests of the shapes of the tax contract: factories, the total, masking, enum values and the capability.
/// </summary>
public sealed class TaxContractShapeTests
{
    /// <summary>A calculation carries its lines, no reason, and the sum of the line amounts as its total.</summary>
    [Fact]
    public void Calculated_CarriesLinesAndTotal()
    {
        var result = TaxFixtures.Correct(TaxFixtures.Request);

        result.Outcome.Should().Be(TaxOutcome.Calculated);
        result.Reason.Should().BeNull();
        result.Lines.Should().HaveCount(2);
        result.Total.Should().Be(30.00m);
    }

    /// <summary>A calculation of no lines has a total of zero, which is different from having no total.</summary>
    [Fact]
    public void Calculated_NoLines_TotalIsZero()
    {
        TaxCalculationResult.Calculated([]).Total.Should().Be(0m);
    }

    /// <summary>A calculation without lines is refused when it is built.</summary>
    [Fact]
    public void Calculated_NullLines_Throws()
    {
        var build = () => TaxCalculationResult.Calculated(null!);

        build.Should().Throw<ArgumentNullException>();
    }

    /// <summary>A refusal carries its reason and no lines, so it has no total.</summary>
    [Fact]
    public void Refused_CarriesReasonOnly()
    {
        var result = TaxCalculationResult.Refused(TaxRefusalReason.TaxIdInvalid);

        result.Outcome.Should().Be(TaxOutcome.Refused);
        result.Reason.Should().Be(TaxRefusalReason.TaxIdInvalid);
        result.Lines.Should().BeNull();
        result.Total.Should().BeNull();
    }

    /// <summary>An unavailable service carries neither reason nor lines, so it never reads as zero tax.</summary>
    [Fact]
    public void Unavailable_CarriesNothing()
    {
        var result = TaxCalculationResult.Unavailable();

        result.Outcome.Should().Be(TaxOutcome.Unavailable);
        result.Reason.Should().BeNull();
        result.Lines.Should().BeNull();
        result.Total.Should().BeNull();
    }

    /// <summary>The request prints without the tax number and the address details, and keeps the country.</summary>
    [Fact]
    public void Request_ToString_MasksPersonalData()
    {
        var printed = TaxFixtures.Request.ToString();

        printed.Should().NotContain(TaxFixtures.BuyerTaxId).And.NotContain(TaxFixtures.BuyerStreet);
        printed.Should().NotContain("75001").And.NotContain("Paris");
        printed.Should().Contain("TaxId = ***").And.Contain("Country = FR");
    }

    /// <summary>An address prints a missing region, postal code, city and street all as null.</summary>
    [Fact]
    public void Address_ToString_PrintsMissingMembersAsNull()
    {
        var printed = new TaxAddress("US", null, null, null, null).ToString();

        printed.Should().Be("TaxAddress { Country = US, Region = null, PostalCode = null, City = null, Line1 = null }");
    }

    /// <summary>A calculation refuses a null entry among its lines when it is built.</summary>
    [Fact]
    public void Calculated_NullEntry_Throws()
    {
        var build = () => TaxCalculationResult.Calculated(new TaxLineResult[] { null! });

        build.Should().Throw<ArgumentException>();
    }

    /// <summary>A party without a tax number prints it as null and not as a mask.</summary>
    [Fact]
    public void Party_WithoutTaxId_PrintsNull()
    {
        var party = new TaxParty(new TaxAddress("US", "CA", null, null, null), false, null);

        party.ToString().Should().Contain("TaxId = null").And.Contain("Region = CA");
    }

    /// <summary>
    /// Equality still sees the masked members, so two parties that differ only in the tax number differ.
    /// </summary>
    [Fact]
    public void Party_Equality_StillComparesTaxId()
    {
        var first = TaxFixtures.Request.Buyer;
        var second = first with { TaxId = "FR00000000000" };

        first.Should().NotBe(second);
    }

    /// <summary>
    /// The tax outcomes and refusal reasons are pinned: values are appended, never renumbered, and zero is invalid.
    /// </summary>
    [Fact]
    public void Enums_Values_ArePinned()
    {
        Enum.GetValues<TaxOutcome>().Select(value => (int)value).Should().Equal(1, 2, 3);
        Enum.IsDefined((TaxOutcome)0).Should().BeFalse();
        Enum.GetValues<TaxRefusalReason>().Select(value => value.ToString()).Should().Equal(
            "AddressUnsupported", "TaxIdInvalid", "CurrencyNotSupported", "CategoryUnknown", "AccessDenied", "Other");
        Enum.GetValues<TaxRefusalReason>().Select(value => (int)value).Should().Equal(1, 2, 3, 4, 5, 6);
        Enum.IsDefined((TaxRefusalReason)0).Should().BeFalse();
    }

    /// <summary>The tax plugin type is appended after the others and the capability has its manifest name.</summary>
    [Fact]
    public void PluginTypeAndCapability_AreNamed()
    {
        ((int)PluginType.Tax).Should().Be(7);
        PluginCapabilities.Tax.Should().Be("tax");
    }
}
