namespace Innovayse.SDK.Plugins.StoredCredentials;

/// <summary>Input for <see cref="IStoredCredentialPlugin.ChargeAsync"/>.</summary>
/// <param name="CustomerRef">The customer reference from <see cref="CredentialCustomerResult"/>.</param>
/// <param name="MethodRef">The stored method to charge, from <see cref="SavedMethod"/>.</param>
/// <param name="AmountMinor">Amount in minor currency units. Always positive; the host checks <see cref="IPaymentPlugin.AcceptsCurrency"/> for the currency before it calls <see cref="IStoredCredentialPlugin.ChargeAsync"/>.</param>
/// <param name="Currency">ISO 4217 numeric code of the currency, in the same form as <see cref="IPaymentPlugin.CurrencyCode"/>.</param>
/// <param name="InvoiceReference">The host's invoice reference, for the gateway's records and the payer's statement.</param>
/// <param name="IdempotencyKey">
/// Opaque ASCII key, at most 64 characters, created once per attempt. A retry of the same attempt reuses it and a
/// new attempt after a confirmed failure gets a new one. The plugin passes it to the gateway so a repeated call
/// never charges twice, or looks the key up itself first when the gateway has no native idempotency.
/// </param>
/// <param name="ReturnUrl">Absolute URL the payer returns to after authenticating, used when the outcome is <see cref="Enums.StoredChargeOutcome.ActionRequired"/>.</param>
public sealed record StoredChargeInstruction(
    string CustomerRef,
    string MethodRef,
    long AmountMinor,
    string Currency,
    string InvoiceReference,
    string IdempotencyKey,
    string? ReturnUrl);
