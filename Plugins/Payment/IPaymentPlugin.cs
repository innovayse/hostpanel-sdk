namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>
/// Contract for redirect-based payment gateway plugins (hosted payment page model).
/// Implement via <see cref="Base.PaymentGatewayBase"/>.
/// </summary>
public interface IPaymentPlugin
{
    /// <summary>
    /// Gets the ISO 4217 numeric currency code (e.g. "840" for USD, "051" for AMD) that this
    /// plugin instance will charge in, as configured by the admin. Callers must verify this
    /// matches the currency of the invoice being paid before calling <see cref="CreatePaymentAsync"/>:
    /// the gateway itself has no notion of the host's billing currency and will happily charge
    /// the requested minor-unit amount in whatever currency it is configured for.
    /// </summary>
    string CurrencyCode { get; }

    /// <summary>
    /// Whether this plugin can charge an invoice in the given currency. Defaults to an exact match with
    /// <see cref="CurrencyCode"/>; a multi-currency gateway overrides it.
    /// </summary>
    /// <param name="currencyNumeric">ISO 4217 numeric code of the invoice currency.</param>
    /// <returns><see langword="true"/> when the plugin charges in that currency.</returns>
    bool AcceptsCurrency(string currencyNumeric) => string.Equals(currencyNumeric, CurrencyCode, StringComparison.Ordinal);

    /// <summary>Registers a payment at the gateway and returns the redirect session.</summary>
    /// <param name="instruction">Order number, amount in minor units, return URL and optional metadata.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The gateway-side order id and the URL to redirect the payer to.</returns>
    Task<PaymentSession> CreatePaymentAsync(PaymentInstruction instruction, CancellationToken ct);

    /// <summary>Queries the gateway for the current state of a previously created payment.</summary>
    /// <param name="gatewayOrderId">The gateway-side order id returned by <see cref="CreatePaymentAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mapped payment state with the gateway transaction reference when paid.</returns>
    Task<GatewayPaymentStatus> GetStatusAsync(string gatewayOrderId, CancellationToken ct);

    /// <summary>Refunds a captured payment, fully or partially.</summary>
    /// <param name="gatewayOrderId">The gateway-side order id of the paid payment.</param>
    /// <param name="amountMinor">Refund amount in minor currency units.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A gateway reference for the refund operation.</returns>
    Task<string> RefundAsync(string gatewayOrderId, long amountMinor, CancellationToken ct);

    /// <summary>
    /// Refunds a captured payment, fully or partially, under a host-supplied stable refund id. This is the overload
    /// the host calls. A plugin overrides it to pass <see cref="RefundInstruction.RefundId"/> to the gateway as the
    /// idempotency key; a plugin that does not is still called, and the default falls back to
    /// <see cref="RefundAsync(string, long, CancellationToken)"/> without the id, so its refunds are not protected
    /// against a repeat.
    /// <para>
    /// Retry semantics. The host stores the refund id before the first call and reuses it for every retry of the same
    /// refund, after a timeout, a crash or an exception. A repeated call with the same id must never refund twice and
    /// should answer the same gateway reference as the first call. A gateway without native idempotency keys needs the
    /// plugin to look the id up in its own store first. A refusal by the gateway throws, as with the other overload.
    /// </para>
    /// <para>
    /// Retry without a plugin store. <see cref="RefundInstruction.AlreadyRefundedMinor"/> is what the host has already
    /// recorded as refunded through the order before this refund. A gateway that reports the total it has refunded on
    /// an order can use it instead of storage: when that total is at least
    /// <c>AlreadyRefundedMinor + AmountMinor</c> the refund already happened (a retry), so skip it and answer the
    /// reference of the earlier refund; otherwise send it. The default implementation below cannot apply the rule, as
    /// the older overload has no such value, and ignores it.
    /// </para>
    /// </summary>
    /// <param name="instruction">The order, the amount in minor units and the stable refund id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A gateway reference for the refund operation; the same value on a repeat of the same refund id.</returns>
    Task<string> RefundAsync(RefundInstruction instruction, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        ArgumentOutOfRangeException.ThrowIfNegative(instruction.AlreadyRefundedMinor);
        return RefundAsync(instruction.GatewayOrderId, instruction.AmountMinor, ct);
    }
}
