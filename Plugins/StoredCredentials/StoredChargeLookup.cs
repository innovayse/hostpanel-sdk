using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.StoredCredentials;

/// <summary>
/// Result of <see cref="IStoredCredentialPlugin.FindChargeAsync"/>: whether an idempotency key ever charged, and how
/// that charge stands now. It is the recovery path after a timeout or a crash, and the way the host resolves the
/// final outcome of a charge that needed the payer to authenticate.
/// </summary>
/// <param name="Found">
/// Whether the gateway knows a charge made with the key. <see langword="false"/> is trusted only when the lookup
/// itself succeeded; a lookup that could not be answered throws instead.
/// </param>
/// <param name="Outcome">The current outcome of the charge; null when <paramref name="Found"/> is false.</param>
/// <param name="TransactionId">The gateway transaction reference; set when the charge succeeded.</param>
/// <param name="AmountMinor">The amount taken, in minor units; set when the charge succeeded.</param>
/// <param name="Currency">ISO 4217 numeric code of the currency charged; set when the charge succeeded.</param>
/// <param name="ActionUrl">Where to send the payer to authenticate; set only while <paramref name="Outcome"/> is <see cref="StoredChargeOutcome.ActionRequired"/>, so the host can offer the page again.</param>
public sealed record StoredChargeLookup(
    bool Found,
    StoredChargeOutcome? Outcome,
    string? TransactionId,
    long? AmountMinor,
    string? Currency,
    string? ActionUrl);
