using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Payment;

/// <summary>Output of <see cref="IPaymentCancelPlugin.CancelPaymentAsync"/>.</summary>
/// <param name="Outcome">How the cancellation ended.</param>
/// <param name="GatewayReference">The gateway reference of the cancellation, or <see langword="null"/> when the gateway gives none.</param>
public sealed record PaymentCancelResult(PaymentCancelOutcome Outcome, string? GatewayReference);
