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
public sealed record PaymentInstruction(
    string OrderNumber,
    long AmountMinor,
    string ReturnUrl,
    string? Description,
    string? Language,
    bool SavePaymentMethod = false);
