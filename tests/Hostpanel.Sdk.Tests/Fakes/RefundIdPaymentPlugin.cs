using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Payment;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A payment plugin that overrides the refund overload with an id and, like a gateway with idempotency keys, answers the
/// same reference for a repeated refund id. It also cancels payments, so one class covers both optional capabilities.
/// </summary>
internal sealed class RefundIdPaymentPlugin : IPaymentCancelPlugin
{
    /// <summary>The reference first answered for each refund id.</summary>
    private readonly Dictionary<string, string> _refunds = [];

    /// <summary>The order ids already cancelled.</summary>
    private readonly HashSet<string> _cancelled = [];

    /// <summary>Gets how many refunds the fake gateway really executed (a repeated id is not executed again).</summary>
    public int ExecutedRefunds => _refunds.Count;

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
        throw new InvalidOperationException("The host must call the overload that carries the refund id.");
    }

    /// <inheritdoc />
    public Task<string> RefundAsync(RefundInstruction instruction, CancellationToken ct)
    {
        if (!_refunds.TryGetValue(instruction.RefundId, out var reference))
        {
            reference = "refund-" + (_refunds.Count + 1);
            _refunds[instruction.RefundId] = reference;
        }

        return Task.FromResult(reference);
    }

    /// <inheritdoc />
    public Task<PaymentCancelResult> CancelPaymentAsync(string gatewayOrderId, CancellationToken ct)
    {
        var outcome = _cancelled.Add(gatewayOrderId) ? PaymentCancelOutcome.Cancelled : PaymentCancelOutcome.AlreadyCancelled;
        return Task.FromResult(new PaymentCancelResult(outcome, "cancel-" + gatewayOrderId));
    }
}
