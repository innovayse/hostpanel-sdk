using Innovayse.SDK.Plugins.Identity;

namespace Innovayse.SDK.Plugins.Fulfilment;
/// <summary>
/// Handed to <see cref="IFulfilmentPlugin.OnInvoicePaidAsync"/> once an invoice with a digital line is paid.
/// </summary>
/// <param name="InvoiceId">The paid invoice. Plugins key idempotency on it — delivery is at-least-once.</param>
/// <param name="ClientId">The host's client id.</param>
/// <param name="Account">The client's external account, or null when the client is not linked to one.</param>
/// <param name="Lines">Every product line on the invoice; a plugin ignores the ones it does not fulfil.</param>
/// <param name="TransactionReference">The gateway's transaction reference, or null for a manual payment.</param>
/// <param name="PaidAt">When the payment was recorded (UTC).</param>
public sealed record InvoicePaidContext(
    int InvoiceId,
    int ClientId,
    ExternalAccount? Account,
    IReadOnlyList<FulfilmentLine> Lines,
    string? TransactionReference,
    DateTimeOffset PaidAt);
