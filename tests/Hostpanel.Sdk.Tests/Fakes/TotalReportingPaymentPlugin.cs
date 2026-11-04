using Hostpanel.Sdk.Plugins.Payment;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A payment plugin for a gateway with no idempotency key and no plugin storage: the gateway only reports how much it has
/// refunded on an order, and the plugin decides retry or new refund from <see cref="RefundInstruction.AlreadyRefundedMinor"/>.
/// </summary>
internal sealed class TotalReportingPaymentPlugin : IPaymentPlugin
{
    /// <summary>Gets what the fake gateway has refunded on the order, in minor units.</summary>
    public long RefundedTotal { get; private set; }

    /// <summary>Gets how many refunds were really sent to the fake gateway.</summary>
    public int Sent { get; private set; }

    /// <inheritdoc />
    public string CurrencyCode => "051";

    /// <inheritdoc />
    public Task<PaymentSession> CreatePaymentAsync(PaymentInstruction instruction, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<GatewayPaymentStatus> GetStatusAsync(string gatewayOrderId, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<string> RefundAsync(string gatewayOrderId, long amountMinor, CancellationToken ct)
    {
        throw new InvalidOperationException("The host must call the overload that carries the instruction.");
    }

    /// <inheritdoc />
    public Task<string> RefundAsync(RefundInstruction instruction, CancellationToken ct)
    {
        var target = instruction.AlreadyRefundedMinor + instruction.AmountMinor;
        if (RefundedTotal < target)
        {
            RefundedTotal += instruction.AmountMinor;
            Sent++;
        }

        return Task.FromResult("refund-" + target);
    }
}
