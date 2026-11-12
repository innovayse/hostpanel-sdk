using Hostpanel.Sdk.Plugins.Payment;

namespace Hostpanel.Sdk.Tests.Payment;

/// <summary>Tests of the optional payer e-mail address on <see cref="PaymentInstruction"/>.</summary>
public sealed class PayerEmailTests
{
    /// <summary>The address the tests use, which no printed form may show.</summary>
    private const string Email = "payer@example.test";

    /// <summary>An instruction built the old way, with five or seven arguments, binds and carries no address.</summary>
    [Fact]
    public void PaymentInstruction_BuiltTheOldWay_HasNoPayerEmail()
    {
        var oldest = new PaymentInstruction("order-1", 1000, "https://host.example/return", null, null);
        var previous = new PaymentInstruction(
            "order-1", 1000, "https://host.example/return", null, null, true, "cus_1");

        oldest.PayerEmail.Should().BeNull();
        previous.PayerEmail.Should().BeNull();
        previous.CustomerRef.Should().Be("cus_1");
    }

    /// <summary>An instruction built with the address carries it and keeps the other members.</summary>
    [Fact]
    public void PaymentInstruction_BuiltWithTheAddress_CarriesIt()
    {
        var instruction = new PaymentInstruction(
            "order-1", 1000, "https://host.example/return", "Invoice 7", "en", PayerEmail: Email);

        instruction.PayerEmail.Should().Be(Email);
        instruction.Description.Should().Be("Invoice 7");
        instruction.SavePaymentMethod.Should().BeFalse();
    }

    /// <summary>The printed form masks the address and still shows the other members.</summary>
    [Fact]
    public void PaymentInstruction_ToString_MasksTheAddress()
    {
        var instruction = new PaymentInstruction(
            "order-1", 1000, "https://host.example/return", null, null, PayerEmail: Email);

        var printed = instruction.ToString();

        printed.Should().NotContain(Email);
        printed.Should().Contain("PayerEmail = ***").And.Contain("OrderNumber = order-1");
    }

    /// <summary>A missing address prints as null, not as a mask.</summary>
    [Fact]
    public void PaymentInstruction_ToString_PrintsMissingAddressAsNull()
    {
        var instruction = new PaymentInstruction("order-1", 1000, "https://host.example/return", null, null);

        instruction.ToString().Should().Contain("PayerEmail = null");
    }

    /// <summary>Equality still sees the address, so two instructions that differ only in it are not equal.</summary>
    [Fact]
    public void PaymentInstruction_Equality_StillComparesTheAddress()
    {
        var first = new PaymentInstruction("order-1", 1000, "https://host.example/return", null, null);

        (first with { PayerEmail = Email }).Should().NotBe(first);
    }
}
