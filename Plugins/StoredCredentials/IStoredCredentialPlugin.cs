namespace Innovayse.SDK.Plugins.StoredCredentials;

/// <summary>
/// Optional capability of a payment plugin: it can keep payment methods at the gateway and charge them later
/// without the payer present. A plugin opts in by implementing this interface on its payment class beside
/// <see cref="IPaymentPlugin"/>, which does not change; the host checks for it at run time and the manifest lists
/// <see cref="PluginCapabilities.StoredCredentials"/>. A disabled plugin, or one without this interface, has no
/// capability.
/// </summary>
/// <remarks>
/// <para>
/// Outcome rules. <see cref="Enums.StoredChargeOutcome.Succeeded"/> means the money is taken and the transaction id,
/// amount and currency are reported. <see cref="Enums.StoredChargeOutcome.Declined"/> is final.
/// <see cref="Enums.StoredChargeOutcome.ActionRequired"/> means the payer must authenticate.
/// <see cref="Enums.StoredChargeOutcome.Pending"/> is accepted but not settled: the host never treats it as paid and
/// resolves it later through <see cref="FindChargeAsync"/>. A refusal is a <c>Declined</c> outcome, never an exception;
/// a plugin throws only when the outcome is unknown, for example when the gateway could not be reached.
/// </para>
/// <para>
/// Idempotency. The host creates the key once per attempt and stores it before calling the plugin. A retry of the
/// same attempt reuses it, so <see cref="ChargeAsync"/> called twice with the same key must never charge twice and
/// should answer what the first call answered. The key is opaque ASCII of at most 64 characters.
/// <see cref="FindChargeAsync"/> answers whether a key ever charged.
/// </para>
/// <para>
/// The host keeps consent, the per invoice claim and the renewal policy; the plugin holds only the gateway part.
/// Saved methods are not stored by the host: <see cref="ListMethodsAsync"/> is read live.
/// </para>
/// </remarks>
public interface IStoredCredentialPlugin
{
    /// <summary>Finds or creates the gateway customer for a client. Safe to call repeatedly with the same client reference.</summary>
    /// <param name="request">The host's client reference and optional contact details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The gateway customer reference.</returns>
    Task<CredentialCustomerResult> EnsureCustomerAsync(CredentialCustomerDetails request, CancellationToken ct);

    /// <summary>Opens a session in which the payer enters a method to store.</summary>
    /// <param name="request">The customer and, for the redirect mode, the return address.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The session, with the client token or the redirect URL according to its mode.</returns>
    Task<SaveMethodSession> BeginSaveMethodAsync(SaveMethodContext request, CancellationToken ct);

    /// <summary>Finishes a session after the payer entered the method, and reads what was stored.</summary>
    /// <param name="sessionId">The session id from <see cref="SaveMethodSession.SessionId"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Whether a method was stored and which.</returns>
    Task<SavedMethodResult> CompleteSaveMethodAsync(string sessionId, CancellationToken ct);

    /// <summary>
    /// Charges a stored method. Idempotent on <see cref="StoredChargeInstruction.IdempotencyKey"/>: a repeated call
    /// never charges twice.
    /// </summary>
    /// <param name="request">The customer, method, amount, currency, invoice reference and idempotency key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>How the charge ended. A refusal is a <see cref="Enums.StoredChargeOutcome.Declined"/> result, not an exception.</returns>
    Task<StoredChargeResult> ChargeAsync(StoredChargeInstruction request, CancellationToken ct);

    /// <summary>Answers whether an idempotency key ever charged, and how that charge stands now. The recovery path after a timeout or a crash.</summary>
    /// <param name="customerRef">The customer reference the charge was made for.</param>
    /// <param name="idempotencyKey">The key the charge was made with.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The lookup; <c>Found = false</c> is trusted only when the lookup itself succeeded, otherwise throw.</returns>
    Task<StoredChargeLookup> FindChargeAsync(string customerRef, string idempotencyKey, CancellationToken ct);

    /// <summary>Lists the customer's stored methods, live from the gateway.</summary>
    /// <param name="customerRef">The customer reference.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The stored methods, the default one flagged.</returns>
    Task<IReadOnlyList<SavedMethod>> ListMethodsAsync(string customerRef, CancellationToken ct);

    /// <summary>Makes a stored method the customer's default.</summary>
    /// <param name="customerRef">The customer reference.</param>
    /// <param name="methodRef">The method to make the default.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the default is set.</returns>
    Task SetDefaultMethodAsync(string customerRef, string methodRef, CancellationToken ct);

    /// <summary>Removes a stored method at the gateway.</summary>
    /// <param name="customerRef">The customer reference.</param>
    /// <param name="methodRef">The method to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the method is removed.</returns>
    Task RemoveMethodAsync(string customerRef, string methodRef, CancellationToken ct);
}
