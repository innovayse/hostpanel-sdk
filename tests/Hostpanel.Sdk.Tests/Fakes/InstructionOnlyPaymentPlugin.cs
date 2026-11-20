using Hostpanel.Sdk.Plugins.Payment;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A payment plugin written for the current contract: it overrides only the refund overload that takes an instruction and
/// carries no legacy overload, and it keeps the instruction it received.
/// </summary>
internal sealed class InstructionOnlyPaymentPlugin : IPaymentPlugin
{
    /// <summary>Gets the instruction of the last refund, or <see langword="null"/> when none was made.</summary>
    public RefundInstruction? LastRefund { get; private set; }

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
    public Task<string> RefundAsync(RefundInstruction instruction, CancellationToken ct)
    {
        LastRefund = instruction;
        return Task.FromResult("refund-" + instruction.RefundId);
    }
}
