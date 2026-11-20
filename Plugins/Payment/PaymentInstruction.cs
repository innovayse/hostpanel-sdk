using Hostpanel.Sdk.Plugins.Diagnostics;

namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>
/// Input for <see cref="IPaymentPlugin.CreatePaymentAsync"/>. The members after <see cref="PayerEmail"/> are init
/// properties with defaults and not constructor parameters, so the constructor is the one every earlier version had and a
/// plugin built on it still binds. Set them with an object initializer.
/// </summary>
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
    [property: SecretMember] string? PayerEmail = null)
{
    /// <summary>
    /// Gets the client's billing country as an ISO 3166-1 alpha-2 code such as <c>AM</c>, for a gateway that needs it for tax
    /// or risk checks. <see langword="null"/> when the host knows none. Optional and null by default. Since 3.11.0.
    /// </summary>
    public string? BillingCountry { get; init; }

    /// <summary>
    /// Gets the full name of the payer, or the company name for a business client, for a gateway that requires one. Personal
    /// data: send it only to your own gateway and never log it; the generated <c>ToString</c> prints it as <c>***</c>.
    /// <see langword="null"/> when the host knows none. Optional and null by default. Since 3.11.0.
    /// </summary>
    [SecretMember]
    public string? PayerName { get; init; }

    /// <summary>
    /// Gets the payer's phone number as the client entered it, for a gateway that requires one. Personal data, handled like
    /// <see cref="PayerName"/>. <see langword="null"/> when the host knows none. Optional and null by default. Since 3.11.0.
    /// </summary>
    [SecretMember]
    public string? PayerPhone { get; init; }

    /// <summary>
    /// Gets the client's tax or VAT identification number, for a gateway that issues a fiscal receipt or invoice. Personal
    /// data, handled like <see cref="PayerName"/>. <see langword="null"/> when the client has none on file. Optional and null
    /// by default. Since 3.11.0.
    /// </summary>
    [SecretMember]
    public string? ClientTaxId { get; init; }

    /// <summary>
    /// Gets when the invoice being paid falls due, for a gateway that shows a deadline or expires the payment with it.
    /// <see langword="null"/> when the payment has no deadline. Optional and null by default. Since 3.11.0.
    /// </summary>
    public DateTimeOffset? DueAt { get; init; }

    /// <summary>
    /// Gets the lines of the invoice being paid, for a gateway that prints or reports them. <see langword="null"/> (the
    /// default) when the host sends none, which is different from an empty list only in that the host did not say; a plugin
    /// treats both as "no lines". Record equality compares the list by reference, so two instructions with equal lines in
    /// different lists are not equal. Since 3.11.0.
    /// </summary>
    public IReadOnlyList<PaymentInvoiceLine>? Lines { get; init; }

    /// <summary>
    /// Gets the absolute public base URL under which the host serves this plugin's <c>api.callbacks</c> routes, with no
    /// trailing slash, for example <c>https://panel.example/api/plugins/acme-pay/callbacks</c>. A plugin appends a declared
    /// callback route to it to tell the gateway where to send notifications, so it never has to guess its own public address.
    /// It sits on the instruction because <see cref="IPaymentPlugin.CreatePaymentAsync"/> is the one call that has no
    /// <see cref="Endpoints.PluginCall"/> and so no host API. It is not a secret. <see langword="null"/> (the default) when
    /// the host does not supply it; a plugin that needs it then keeps its own setting. Since 3.11.0.
    /// </summary>
    public string? CallbackBaseUrl { get; init; }
}
