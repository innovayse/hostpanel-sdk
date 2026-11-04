namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>
/// Optional capability of a payment plugin: it can cancel (void) a payment that was created but not settled, for
/// example when the invoice is cancelled or paid another way while the payer still has the gateway page open. A plugin
/// opts in by implementing this interface on its payment class beside <see cref="IPaymentPlugin"/>, which does not
/// change, so a plugin written before it compiles and loads unchanged; the host checks for the interface at run time
/// and a plugin without it is simply never asked to cancel.
/// </summary>
public interface IPaymentCancelPlugin : IPaymentPlugin
{
    /// <summary>
    /// Cancels an unsettled payment at the gateway. Idempotent: asking again for a payment already cancelled answers
    /// <see cref="Enums.PaymentCancelOutcome.AlreadyCancelled"/> and never fails. A payment that can no longer be
    /// cancelled answers <see cref="Enums.PaymentCancelOutcome.NotCancellable"/>, which is a result, not an exception;
    /// throw only when the outcome is unknown, for example when the gateway could not be reached.
    /// </summary>
    /// <param name="gatewayOrderId">The gateway-side order id returned by <see cref="IPaymentPlugin.CreatePaymentAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>How the cancellation ended, with the gateway reference when there is one.</returns>
    Task<PaymentCancelResult> CancelPaymentAsync(string gatewayOrderId, CancellationToken ct);
}
