namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// How a payer is taken through the hosted payment step of a <see cref="Payment.PaymentSession"/>: the three ways share
/// one value, because a session is exactly one of them. The values start at 1 and 0 is not valid. New values are only
/// ever appended.
/// </summary>
public enum PaymentSessionKind
{
    /// <summary>
    /// The payer is sent to <see cref="Payment.PaymentSession.RedirectUrl"/> with a plain GET. The host reads the
    /// payment status when the payer comes back. This is the kind of every session built before this value existed.
    /// </summary>
    Redirect = 1,

    /// <summary>
    /// The payer's browser posts the form described by <see cref="Payment.PaymentSession.FormPost"/> to the gateway's
    /// hosted page. A session with a form post is always of this kind.
    /// </summary>
    FormPost = 2,

    /// <summary>
    /// The payer completes a step in the browser (for example a card field that produces a payment method nonce) on the
    /// page of <see cref="Payment.PaymentSession.RedirectUrl"/> before the payment can be charged. The host waits for
    /// the return request, calls <see cref="Payment.IPaymentReturnPlugin.CompleteAsync"/> with its parameters and only
    /// then reads the payment status. A plugin that answers this kind implements
    /// <see cref="Payment.IPaymentReturnPlugin"/> and lists <see cref="Manifest.PluginCapabilities.PaymentReturn"/>.
    /// </summary>
    BrowserStep = 3,
}
