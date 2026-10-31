using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.StoredCredentials;

/// <summary>
/// Result of <see cref="IStoredCredentialPlugin.ChargeAsync"/>. A refusal is a <see cref="StoredChargeOutcome.Declined"/>
/// result, never an exception; a plugin throws only when the outcome is unknown.
/// </summary>
/// <param name="Outcome">How the charge ended.</param>
/// <param name="TransactionId">The gateway transaction reference; set whenever <paramref name="Outcome"/> is <see cref="StoredChargeOutcome.Succeeded"/>.</param>
/// <param name="AmountMinor">The amount the gateway says it took, in minor units; reported on success so the host can refuse a mismatch.</param>
/// <param name="Currency">ISO 4217 numeric code of the currency the gateway says it charged in; reported on success so the host can refuse a mismatch.</param>
/// <param name="ActionUrl">Where to send the payer to authenticate; set only for <see cref="StoredChargeOutcome.ActionRequired"/>.</param>
public sealed record StoredChargeResult(
    StoredChargeOutcome Outcome,
    string? TransactionId,
    long? AmountMinor,
    string? Currency,
    string? ActionUrl);
