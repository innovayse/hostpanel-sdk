using Innovayse.SDK.Plugins.Identity;

namespace Innovayse.SDK.Plugins.Fulfilment;

/// <summary>Handed to <see cref="IFulfilmentPlugin.OnInvoiceRefundedAsync"/> after the money moved back.</summary>
/// <param name="InvoiceId">The refunded invoice.</param>
/// <param name="ClientId">The host's client id.</param>
/// <param name="Account">The client's external account, or null.</param>
/// <param name="Lines">Every product line on the invoice.</param>
/// <param name="RefundedAmount">What was refunded, in <paramref name="Currency"/>.</param>
/// <param name="Currency">ISO 4217 alpha code of the invoice.</param>
/// <param name="TransactionReference">The refund's gateway reference, or null.</param>
/// <param name="RefundedAt">When the refund was recorded (UTC).</param>
public sealed record InvoiceRefundedContext(
    int InvoiceId,
    int ClientId,
    ExternalAccount? Account,
    IReadOnlyList<FulfilmentLine> Lines,
    decimal RefundedAmount,
    string Currency,
    string? TransactionReference,
    DateTimeOffset RefundedAt);
