using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Payment;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A payment plugin that completes a payment with the nonce the payer's browser returns, the way a nonce style gateway
/// does: the payment is paid once a nonce arrived, and a repeat answers the same status.
/// </summary>
internal sealed class NoncePaymentPlugin : IPaymentReturnPlugin
{
    /// <summary>The name of the return parameter that carries the nonce.</summary>
    public const string NonceName = "payment_method_nonce";

    /// <summary>Holds the gateway order ids that were completed.</summary>
    private readonly HashSet<string> _completed = [];

    /// <summary>Gets how many times a payment was charged, which a repeat of the return must not increase.</summary>
    public int Charges { get; private set; }

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
        return Task.FromResult(StatusOf(gatewayOrderId));
    }

    /// <inheritdoc />
    public Task<string> RefundAsync(string gatewayOrderId, long amountMinor, CancellationToken ct)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public Task<GatewayPaymentStatus> CompleteAsync(
        string gatewayOrderId,
        IReadOnlyDictionary<string, string> returnParameters,
        CancellationToken ct)
    {
        if (returnParameters.ContainsKey(NonceName) && _completed.Add(gatewayOrderId))
        {
            Charges++;
        }

        return Task.FromResult(StatusOf(gatewayOrderId));
    }

    /// <summary>Reads the status of a payment from what was completed.</summary>
    /// <param name="gatewayOrderId">The gateway-side order id.</param>
    /// <returns>Paid when the payment was completed, pending otherwise.</returns>
    private GatewayPaymentStatus StatusOf(string gatewayOrderId)
    {
        return _completed.Contains(gatewayOrderId)
            ? new GatewayPaymentStatus(GatewayPaymentState.Paid, "tx-" + gatewayOrderId, "PAID")
            : new GatewayPaymentStatus(GatewayPaymentState.Pending, null, "PENDING");
    }
}
