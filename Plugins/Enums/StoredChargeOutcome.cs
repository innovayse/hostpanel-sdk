namespace Innovayse.SDK.Plugins.Enums;

/// <summary>
/// How a charge against a stored payment method ended. The values start at 1 and 0 is not valid: a
/// default-initialised value means the plugin set nothing, and the host refuses it. A refusal is always an outcome,
/// never an exception: a plugin throws only when it does not know what happened, for example when the gateway
/// could not be reached.
/// </summary>
public enum StoredChargeOutcome
{
    /// <summary>The money is taken. The result carries the transaction id, the amount and the currency, so the host can refuse a mismatch.</summary>
    Succeeded = 1,

    /// <summary>The gateway refused the charge. Final: the same attempt is never retried, a new attempt gets a new idempotency key.</summary>
    Declined = 2,

    /// <summary>
    /// The payer must authenticate before the charge can complete; the result carries the URL to send them to. After the
    /// payer authenticates, the host resolves the final outcome with
    /// <see cref="Innovayse.SDK.Plugins.StoredCredentials.IStoredCredentialPlugin.FindChargeAsync"/> and the same
    /// idempotency key, so the plugin must report the settled outcome there.
    /// </summary>
    ActionRequired = 3,

    /// <summary>The gateway accepted the charge but has not settled it. The host never treats it as paid and resolves it later through <see cref="Innovayse.SDK.Plugins.StoredCredentials.IStoredCredentialPlugin.FindChargeAsync"/>.</summary>
    Pending = 4,
}
