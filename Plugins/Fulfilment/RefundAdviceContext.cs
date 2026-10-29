using Innovayse.SDK.Plugins.Identity;

namespace Innovayse.SDK.Plugins.Fulfilment;

/// <summary>Handed to <see cref="IFulfilmentPlugin.AdviseRefundAsync"/> before any money moves back.</summary>
/// <param name="InvoiceId">The paid invoice an admin wants to refund.</param>
/// <param name="ClientId">The host's client id.</param>
/// <param name="Account">The client's external account, or null.</param>
/// <param name="Lines">Every product line on the invoice.</param>
/// <param name="RequestedAmount">What the admin asked to refund, in <paramref name="Currency"/>.</param>
/// <param name="Currency">ISO 4217 alpha code of the invoice.</param>
public sealed record RefundAdviceContext(
    int InvoiceId,
    int ClientId,
    ExternalAccount? Account,
    IReadOnlyList<FulfilmentLine> Lines,
    decimal RequestedAmount,
    string Currency);
