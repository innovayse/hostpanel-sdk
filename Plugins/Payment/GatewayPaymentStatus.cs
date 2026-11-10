using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>Result of <see cref="IPaymentPlugin.GetStatusAsync"/>.</summary>
/// <param name="State">The mapped payment state.</param>
/// <param name="TransactionId">Gateway transaction reference; set only when <paramref name="State"/> is <see cref="GatewayPaymentState.Paid"/>.</param>
/// <param name="RawStatus">The gateway's raw status value, for diagnostics.</param>
/// <param name="AmountMinor">
/// The amount the gateway says it took, in minor units; null when the gateway does not report it.
/// The host refuses a paid status whose reported amount differs from what the invoice owes.
/// Optional so a plugin that does not report it still compiles.
/// </param>
/// <param name="Currency">
/// ISO 4217 numeric code of the currency the gateway says it charged in, in the same form as
/// <see cref="IPaymentPlugin.CurrencyCode"/>; null when not reported.
/// The host refuses a paid status in a currency other than the invoice's.
/// </param>
/// <param name="SavedMethod">
/// The payment method the gateway stored for this payment, with its token reference, brand, last four digits and
/// expiry; null when <see cref="PaymentInstruction.SavePaymentMethod"/> was not requested, the plugin does not support
/// it, the gateway did not store the method, or <paramref name="State"/> is not <see cref="GatewayPaymentState.Paid"/>.
/// It is the same <see cref="SavedMethod"/> type that <c>IStoredCredentialPlugin</c> lists, so a method saved here is
/// charged the same way. Optional so a plugin that does not save methods still compiles.
/// </param>
public sealed record GatewayPaymentStatus(
    GatewayPaymentState State,
    string? TransactionId,
    string? RawStatus,
    long? AmountMinor = null,
    string? Currency = null,
    SavedMethod? SavedMethod = null);
