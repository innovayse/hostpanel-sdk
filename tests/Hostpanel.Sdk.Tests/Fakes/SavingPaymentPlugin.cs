using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Payment;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A payment plugin that honours <see cref="PaymentInstruction.SavePaymentMethod"/>: it remembers the flag per gateway order
/// and, once the payment is paid, reports the stored method in <see cref="GatewayPaymentStatus.SavedMethod"/>.
/// </summary>
internal sealed class SavingPaymentPlugin : IPaymentPlugin
{
    /// <summary>The method this fake gateway stores when asked to.</summary>
    public static readonly SavedMethod StoredMethod = new("tok_1", "visa", "4242", 12, 2030, false);

    /// <summary>Holds the gateway order ids whose payment asked for the method to be saved.</summary>
    private readonly HashSet<string> _saving = [];

    /// <inheritdoc />
    public string CurrencyCode => "051";

    /// <inheritdoc />
    public Task<PaymentSession> CreatePaymentAsync(PaymentInstruction instruction, CancellationToken ct)
    {
        var gatewayOrderId = "gw-" + instruction.OrderNumber;
        if (instruction.SavePaymentMethod)
        {
            _saving.Add(gatewayOrderId);
        }

        return Task.FromResult(new PaymentSession(gatewayOrderId, "https://pay.example/" + gatewayOrderId));
    }

    /// <inheritdoc />
    public Task<GatewayPaymentStatus> GetStatusAsync(string gatewayOrderId, CancellationToken ct)
    {
        var method = _saving.Contains(gatewayOrderId) ? StoredMethod : null;
        return Task.FromResult(new GatewayPaymentStatus(GatewayPaymentState.Paid, "tx-1", "PAID", SavedMethod: method));
    }

    /// <inheritdoc />
    public Task<string> RefundAsync(string gatewayOrderId, long amountMinor, CancellationToken ct)
    {
        throw new NotSupportedException();
    }
}
