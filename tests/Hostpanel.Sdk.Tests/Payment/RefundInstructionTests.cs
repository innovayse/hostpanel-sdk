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
