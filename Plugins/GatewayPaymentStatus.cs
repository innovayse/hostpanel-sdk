using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins;

/// <summary>Result of <see cref="IPaymentPlugin.GetStatusAsync"/>.</summary>
/// <param name="State">The mapped payment state.</param>
/// <param name="TransactionId">Gateway transaction reference; set only when <paramref name="State"/> is <see cref="GatewayPaymentState.Paid"/>.</param>
/// <param name="RawStatus">The gateway's raw status value, for diagnostics.</param>
/// <param name="AmountMinor">
/// The amount the gateway says it took, in minor units; null when the gateway does not report it.
/// The panel refuses a paid status whose reported amount differs from what the invoice owes.
/// Optional so a plugin that does not report it still compiles.
/// </param>
/// <param name="CurrencyNumeric">
/// ISO 4217 numeric code of the currency the gateway says it charged in; null when not reported.
/// The panel refuses a paid status in a currency other than the invoice's.
/// </param>
public sealed record GatewayPaymentStatus(
    GatewayPaymentState State,
    string? TransactionId,
    string? RawStatus,
    long? AmountMinor = null,
    string? CurrencyNumeric = null);
