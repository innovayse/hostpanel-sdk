using Hostpanel.Sdk.Plugins.Payment;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Payment;

/// <summary>Tests of <c>IPaymentPlugin.RefundAsync(RefundInstruction, CancellationToken)</c>.</summary>
public sealed class RefundInstructionTests
{
    /// <summary>A plugin written before refund ids, called through the new overload, refunds through its old method.</summary>
    [Fact]
    public async Task RefundAsync_LegacyPlugin_FallsBackToTheOldOverload()
    {
        var legacy = new LegacyPaymentPlugin();
        IPaymentPlugin plugin = legacy;

        var reference = await plugin.RefundAsync(new RefundInstruction("order-1", 1500, "refund-a", 0), CancellationToken.None);

        reference.Should().Be("legacy-order-1");
        legacy.RefundedAmount.Should().Be(1500);
    }

    /// <summary>A plugin that overrides the overload receives the refund id and repeats the same reference for it.</summary>
    [Fact]
    public async Task RefundAsync_RepeatedRefundId_ExecutesOnce()
    {
        var gateway = new RefundIdPaymentPlugin();
        IPaymentPlugin plugin = gateway;
        var instruction = new RefundInstruction("order-1", 500, "refund-a", 0);

        var first = await plugin.RefundAsync(instruction, CancellationToken.None);
        var retry = await plugin.RefundAsync(instruction, CancellationToken.None);
        var other = await plugin.RefundAsync(instruction with { RefundId = "refund-b" }, CancellationToken.None);

        retry.Should().Be(first);
        other.Should().NotBe(first);
        gateway.ExecutedRefunds.Should().Be(2);
    }

    /// <summary>A null instruction is refused by the default implementation.</summary>
    [Fact]
    public async Task RefundAsync_NullInstruction_Throws()
    {
        IPaymentPlugin plugin = new LegacyPaymentPlugin();

        var call = () => plugin.RefundAsync(null!, CancellationToken.None);

        await call.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>A negative already-refunded sum is refused by the default implementation.</summary>
    [Fact]
    public async Task RefundAsync_NegativeAlreadyRefunded_Throws()
    {
        IPaymentPlugin plugin = new LegacyPaymentPlugin();

        var call = () => plugin.RefundAsync(new RefundInstruction("order-1", 100, "refund-a", -1), CancellationToken.None);

        await call.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>A plugin that overrides neither overload refuses the refund with <see cref="NotSupportedException"/>.</summary>
    [Fact]
    public async Task RefundAsync_NeitherOverloadOverridden_ThrowsNotSupported()
    {
        IPaymentPlugin plugin = new RefundlessPaymentPlugin();

        var legacy = () => plugin.RefundAsync("order-1", 100, CancellationToken.None);
        var current = () => plugin.RefundAsync(new RefundInstruction("order-1", 100, "refund-a", 0), CancellationToken.None);

        await legacy.Should().ThrowExactlyAsync<NotSupportedException>();
        await current.Should().ThrowExactlyAsync<NotSupportedException>();
    }

    /// <summary>A plugin that overrides only the instruction overload works, and the legacy overload it does not carry refuses.</summary>
    [Fact]
    public async Task RefundAsync_InstructionOverloadOnly_WorksAndLegacyRefuses()
    {
        var gateway = new InstructionOnlyPaymentPlugin();
        IPaymentPlugin plugin = gateway;

        var reference = await plugin.RefundAsync(new RefundInstruction("order-1", 100, "refund-a", 0), CancellationToken.None);
        var legacy = () => plugin.RefundAsync("order-1", 100, CancellationToken.None);

        reference.Should().Be("refund-refund-a");
        await legacy.Should().ThrowExactlyAsync<NotSupportedException>();
    }

    /// <summary>The refusal message names no order, amount or address.</summary>
    [Fact]
    public async Task RefundAsync_LegacyDefault_MessageHasNoOrderData()
    {
        IPaymentPlugin plugin = new RefundlessPaymentPlugin();

        var legacy = () => plugin.RefundAsync("order-secret-77", 4242, CancellationToken.None);

        var thrown = await legacy.Should().ThrowExactlyAsync<NotSupportedException>();
        thrown.Which.Message.Should().NotContain("order-secret-77").And.NotContain("4242");
    }

    /// <summary>The transaction id is optional, defaults to <see langword="null"/>, and the four argument form still compiles.</summary>
    [Fact]
    public void RefundInstruction_TransactionId_DefaultsToNull()
    {
        var instruction = new RefundInstruction("order-1", 100, "refund-a", 0);

        instruction.TransactionId.Should().BeNull();
    }

    /// <summary>The transaction id the host passes reaches the plugin unchanged, and a copy with another refund id keeps it.</summary>
    [Fact]
    public async Task RefundAsync_TransactionId_ReachesThePlugin()
    {
        var gateway = new InstructionOnlyPaymentPlugin();
        IPaymentPlugin plugin = gateway;
        var instruction = new RefundInstruction("order-1", 100, "refund-a", 0) { TransactionId = "tx-9001" };

        await plugin.RefundAsync(instruction, CancellationToken.None);
        await plugin.RefundAsync(instruction with { RefundId = "refund-b" }, CancellationToken.None);

        gateway.LastRefund.Should().NotBeNull();
        gateway.LastRefund!.TransactionId.Should().Be("tx-9001");
        gateway.LastRefund.RefundId.Should().Be("refund-b");
    }

    /// <summary>The constructor keeps the parameters it had in 3.10.0, so a plugin or a host compiled against it still binds and does not fail with a missing method.</summary>
    [Fact]
    public void RefundInstruction_KeepsItsOriginalConstructor()
    {
        var constructor = typeof(RefundInstruction).GetConstructors().Single();

        constructor.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(typeof(string), typeof(long), typeof(string), typeof(long));
    }

    /// <summary>Two instructions are equal only when the transaction id is equal too.</summary>
    [Fact]
    public void RefundInstruction_Equality_IncludesTheTransactionId()
    {
        var withId = new RefundInstruction("order-1", 100, "refund-a", 0) { TransactionId = "tx-1" };

        withId.Should().Be(new RefundInstruction("order-1", 100, "refund-a", 0) { TransactionId = "tx-1" });
        withId.Should().NotBe(new RefundInstruction("order-1", 100, "refund-a", 0));
        withId.Should().NotBe(new RefundInstruction("order-1", 100, "refund-a", 0) { TransactionId = "tx-2" });
    }

    /// <summary>
    /// A gateway without idempotency keys applies the rule with the host's sum: a total at or above the already-refunded
    /// sum plus the amount is a retry and is skipped, anything lower is sent.
    /// </summary>
    [Fact]
    public async Task RefundAsync_GatewayWithoutKeys_SkipsRetryAndSendsNewPartial()
    {
        var gateway = new TotalReportingPaymentPlugin();
        IPaymentPlugin plugin = gateway;

        var first = await plugin.RefundAsync(new RefundInstruction("order-1", 500, "refund-a", 0), CancellationToken.None);
        var retry = await plugin.RefundAsync(new RefundInstruction("order-1", 500, "refund-a", 0), CancellationToken.None);
        var second = await plugin.RefundAsync(new RefundInstruction("order-1", 200, "refund-b", 500), CancellationToken.None);
        var secondRetry = await plugin.RefundAsync(new RefundInstruction("order-1", 200, "refund-b", 500), CancellationToken.None);

        retry.Should().Be(first);
        secondRetry.Should().Be(second);
        gateway.RefundedTotal.Should().Be(700);
        gateway.Sent.Should().Be(2);
    }
}
