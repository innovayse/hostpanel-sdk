using Hostpanel.Sdk.Plugins.Payment;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A payment plugin written before refund ids existed: it implements only the members <see cref="IPaymentPlugin"/> has
/// always required, which proves such a plugin still compiles.
/// </summary>
internal sealed class LegacyPaymentPlugin : IPaymentPlugin
{
    /// <summary>Gets the amount of the last refund, or <see langword="null"/> when none was made.</summary>
    public long? RefundedAmount { get; private set; }

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
        RefundedAmount = amountMinor;
        return Task.FromResult("legacy-" + gatewayOrderId);
    }
}
