using Hostpanel.Sdk.Plugins.Payment;

namespace Hostpanel.Sdk.Tests.Payment;

/// <summary>Tests of the optional payer, tax, deadline, line and callback members of <see cref="PaymentInstruction"/>.</summary>
public sealed class PaymentInstructionDetailsTests
{
    /// <summary>The name, phone and tax id the tests use, which no printed form may show.</summary>
    private static readonly string[] Secrets = ["Ada Payer", "+37411000000", "TAX-123456"];

    /// <summary>An instruction built the old way carries none of the new members.</summary>
    [Fact]
    public void PaymentInstruction_BuiltTheOldWay_HasNoneOfTheNewMembers()
    {
        var instruction = new PaymentInstruction(
            "order-1", 1000, "https://host.example/return", null, null, true, "cus_1", "a@example.test");

        instruction.BillingCountry.Should().BeNull();
        instruction.PayerName.Should().BeNull();
        instruction.PayerPhone.Should().BeNull();
        instruction.ClientTaxId.Should().BeNull();
        instruction.DueAt.Should().BeNull();
        instruction.Lines.Should().BeNull();
        instruction.CallbackBaseUrl.Should().BeNull();
    }

    /// <summary>The constructor keeps the parameters it had in 3.10.0, so a plugin or a host compiled against it still binds and does not fail with a missing method.</summary>
    [Fact]
    public void PaymentInstruction_KeepsItsOriginalConstructor()
    {
        var constructor = typeof(PaymentInstruction).GetConstructors().Single();

        constructor.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(string), typeof(long), typeof(string), typeof(string), typeof(string), typeof(bool), typeof(string), typeof(string));
    }

    /// <summary>An instruction built with the new members carries each of them and keeps the old ones.</summary>
    [Fact]
    public void PaymentInstruction_BuiltWithTheNewMembers_CarriesThem()
    {
        var due = new DateTimeOffset(2030, 1, 31, 0, 0, 0, TimeSpan.Zero);
        PaymentInvoiceLine[] lines = [new("Hosting, 1 month", 1, 5000), new("Support hours", 1.5m, 2000)];

        var instruction = Build(due, lines);

        instruction.BillingCountry.Should().Be("AM");
        instruction.PayerName.Should().Be(Secrets[0]);
        instruction.PayerPhone.Should().Be(Secrets[1]);
        instruction.ClientTaxId.Should().Be(Secrets[2]);
        instruction.DueAt.Should().Be(due);
        instruction.CallbackBaseUrl.Should().Be("https://panel.example/api/plugins/acme-pay/callback");
        instruction.Lines.Should().Equal(lines);
        instruction.OrderNumber.Should().Be("order-1");
    }

    /// <summary>The printed form masks the personal members and still shows the others.</summary>
    [Fact]
    public void PaymentInstruction_ToString_MasksNamePhoneAndTaxId()
    {
        var printed = Build(null, null).ToString();

        foreach (var secret in Secrets)
        {
            printed.Should().NotContain(secret);
        }

        printed.Should().Contain("PayerName = ***").And.Contain("PayerPhone = ***").And.Contain("ClientTaxId = ***");
        printed.Should().Contain("BillingCountry = AM").And.Contain("CallbackBaseUrl = https://panel.example");
    }

    /// <summary>Equality still sees the new members.</summary>
    [Fact]
    public void PaymentInstruction_Equality_ComparesTheNewMembers()
    {
        var first = Build(null, null);

        (first with { PayerName = "Other" }).Should().NotBe(first);
        (first with { CallbackBaseUrl = "https://other.example" }).Should().NotBe(first);
        (first with { }).Should().Be(first);
    }

    /// <summary>An invoice line keeps its description, a fractional quantity and the unit amount in minor units.</summary>
    [Fact]
    public void PaymentInvoiceLine_KeepsItsValues()
    {
        var line = new PaymentInvoiceLine("Support hours", 1.5m, 2000);

        line.Description.Should().Be("Support hours");
        line.Quantity.Should().Be(1.5m);
        line.UnitAmountMinor.Should().Be(2000);
    }

    /// <summary>Builds an instruction with every new member except the due time and the lines, which the caller picks.</summary>
    /// <param name="due">The due time, or <see langword="null"/>.</param>
    /// <param name="lines">The invoice lines, or <see langword="null"/>.</param>
    /// <returns>The instruction.</returns>
    private static PaymentInstruction Build(DateTimeOffset? due, IReadOnlyList<PaymentInvoiceLine>? lines)
    {
        return new PaymentInstruction("order-1", 1000, "https://host.example/return", null, null)
        {
            BillingCountry = "AM",
            PayerName = Secrets[0],
            PayerPhone = Secrets[1],
            ClientTaxId = Secrets[2],
            DueAt = due,
            Lines = lines,
            CallbackBaseUrl = "https://panel.example/api/plugins/acme-pay/callback",
        };
    }
}
