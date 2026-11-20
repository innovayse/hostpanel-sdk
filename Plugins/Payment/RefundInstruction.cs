namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>
/// Input for <see cref="IPaymentPlugin.RefundAsync(RefundInstruction, CancellationToken)"/>. <see cref="TransactionId"/> is an
/// init property and not a constructor parameter, so the constructor is the one every earlier version had.
/// </summary>
/// <param name="GatewayOrderId">The gateway-side order id of the paid payment.</param>
/// <param name="AmountMinor">Refund amount in minor currency units; positive, and not more than what is still refundable.</param>
/// <param name="RefundId">
/// The host's stable id of this refund, opaque ASCII of at most 64 characters. The host creates it once, stores it
/// before the first call and reuses it on every retry of the same refund, so it is the idempotency key: pass it to the
/// gateway as such, or record it yourself when the gateway has none. A different refund of the same payment gets a
/// different id.
/// </param>
/// <param name="AlreadyRefundedMinor">
/// What the host has already recorded as refunded through this gateway order, in minor currency units, before this
/// <see cref="RefundId"/>; zero or more. A gateway with no idempotency key cannot tell a retry of this refund from a new
/// partial one, but it reports how much it has refunded: if that total is at least
/// <c>AlreadyRefundedMinor + AmountMinor</c>, this refund already happened, so skip it and answer its reference;
/// otherwise send it. A gateway that does have an idempotency key can ignore this value.
/// </param>
public sealed record RefundInstruction(string GatewayOrderId, long AmountMinor, string RefundId, long AlreadyRefundedMinor)
{
    /// <summary>
    /// Gets the transaction id the plugin itself reported in <see cref="GatewayPaymentStatus.TransactionId"/> when the payment
    /// became paid, so a refund that needs it does not have to read the order again or keep the id in the plugin's own
    /// store. <see langword="null"/> (the default) when the host holds none: the payment was recorded before the host kept it,
    /// or the plugin reported none. Since 3.11.0; a host built on an earlier version never fills it, so a plugin that relies
    /// on it sets <c>minHostVersion</c> to the first host release that does and keeps its own lookup for a
    /// <see langword="null"/>.
    /// </summary>
    public string? TransactionId { get; init; }
}
