using Hostpanel.Sdk.Plugins.Payment;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A payment plugin that implements only <see cref="IPaymentPlugin.CurrencyCode"/>, creating and reading a payment, and
/// neither refund overload: it compiles because both have default bodies, and a refund through it is refused.
/// </summary>
internal sealed class RefundlessPaymentPlugin : IPaymentPlugin
{
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
}
