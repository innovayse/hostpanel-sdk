using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>Input for <see cref="IPaymentPlugin.CreatePaymentAsync"/>.</summary>
/// <param name="OrderNumber">Merchant-side order number, unique per attempt, gateway limits apply (some gateways accept at most 25 characters).</param>
/// <param name="AmountMinor">Amount in minor currency units (e.g. luma for AMD).</param>
/// <param name="ReturnUrl">Absolute URL the gateway redirects the payer back to.</param>
/// <param name="Description">Optional free-form description; providers sanitize per their rules.</param>
/// <param name="Language">Optional ISO 639-1 language for the payment page; provider default applies when null.</param>
/// <param name="SavePaymentMethod">
/// Asks the gateway to keep the payment method the payer uses on this payment so the host can charge it later. Optional
/// and false by default, so existing callers and plugins are unchanged. A plugin that supports it sends the gateway's
/// own "store this method" request and, once the payment is paid, returns the stored method in
/// <see cref="GatewayPaymentStatus.SavedMethod"/>. A plugin that does not support it ignores the flag and returns no
/// method; the host treats that as "not saved", never as a failure of the payment.
/// </param>
/// <param name="CustomerRef">
/// The gateway-side customer the saved method belongs to: the reference the host got from
/// <see cref="StoredCredentials.IStoredCredentialPlugin.EnsureCustomerAsync"/> and keeps for the client. The host fills it
/// whenever <c>SavePaymentMethod</c> is true, so a plugin attaches the stored method to that customer and a later stored
/// charge for the same reference finds it. Optional and null by default, so existing callers and plugins are unchanged;
/// a plugin that does not save methods ignores it.
/// </param>
/// <param name="PayerEmail">
/// The e-mail address of the client the invoice belongs to, for gateways that refuse to create a payment without one.
/// The host fills it when it knows an address and leaves it <see langword="null"/> otherwise, so null is a normal
/// value and a plugin must cope with it. It is personal data: send it only to your own gateway and never log it. The
/// generated <c>ToString</c> prints it as <c>***</c>, and a plugin must not undo that. Optional and null by default, so
/// existing callers and plugins are unchanged.
/// </param>
public sealed partial record PaymentInstruction(
    string OrderNumber,
    long AmountMinor,
    string ReturnUrl,
    string? Description,
    string? Language,
    bool SavePaymentMethod = false,
    [property: SecretMember] string? CustomerRef = null,
    [property: SecretMember] string? PayerEmail = null);
