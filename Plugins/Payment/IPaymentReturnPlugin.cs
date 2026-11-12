namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>
/// Optional capability of a payment plugin: it completes a payment with a value that only the payer's browser has when it
/// comes back, such as a payment method nonce or the payer id in the return address. A plugin opts in by implementing this
/// interface on its payment class beside <see cref="IPaymentPlugin"/>, which does not change, so a plugin written before
/// it compiles and loads unchanged; the host checks for the interface at run time and never calls a plugin without it.
/// </summary>
public interface IPaymentReturnPlugin : IPaymentPlugin
{
    /// <summary>
    /// Completes a payment from the parameters of the request the payer came back with, and answers the status the
    /// payment is in afterwards. The host calls it before it asks <see cref="IPaymentPlugin.GetStatusAsync"/>, and again
    /// when the payer returns twice, so it must be idempotent: completing a payment that is already completed answers its
    /// status and does not charge again. The parameters are the query of the return request and, for a step done in the
    /// browser, its posted fields. The host chooses which names reach the plugin and keeps within
    /// <see cref="PaymentReturnLimits"/>; it never passes credentials, cookies or headers. The set is read only and the
    /// values come from the payer's browser, so they are untrusted input: check them against the gateway and never trust
    /// them for the amount or the order.
    /// </summary>
    /// <param name="gatewayOrderId">The gateway-side order id returned by <see cref="IPaymentPlugin.CreatePaymentAsync"/>.</param>
    /// <param name="returnParameters">The names and values of the return request; empty when it carried none.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The status of the payment after the attempt to complete it.</returns>
    Task<GatewayPaymentStatus> CompleteAsync(
        string gatewayOrderId,
        IReadOnlyDictionary<string, string> returnParameters,
        CancellationToken ct);
}
