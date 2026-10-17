namespace Innovayse.SDK.Plugins.Fulfilment;

/// <summary>
/// Contract for a fulfilment plugin (<see cref="Enums.PluginType.Fulfilment"/>). The host records a
/// fulfilment row in the payment's own transaction and delivers it here at least once, with
/// retries; every method must therefore be idempotent on <c>InvoiceId</c>. A thrown exception
/// marks the attempt failed and schedules a retry; returning normally marks it done.
/// </summary>
public interface IFulfilmentPlugin
{
    /// <summary>An invoice with at least one digital line was paid.</summary>
    /// <param name="context">The invoice, its client and its lines.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the products are fulfilled.</returns>
    Task OnInvoicePaidAsync(InvoicePaidContext context, CancellationToken ct);

    /// <summary>Called before a refund moves money: how much of this invoice may still be refunded?</summary>
    /// <param name="context">The invoice and the amount the admin asked for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cap; the host takes the smallest cap across plugins.</returns>
    Task<RefundAdvice> AdviseRefundAsync(RefundAdviceContext context, CancellationToken ct);

    /// <summary>The money moved back; reverse the fulfilment.</summary>
    /// <param name="context">The invoice and the refunded amount.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the reversal is done.</returns>
    Task OnInvoiceRefundedAsync(InvoiceRefundedContext context, CancellationToken ct);

    /// <summary>A renewal invoice passed its grace period unpaid.</summary>
    /// <param name="context">The lapsed invoice and its lines.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the lapse is applied.</returns>
    Task OnSubscriptionLapsedAsync(SubscriptionLapsedContext context, CancellationToken ct);
}
