using Innovayse.SDK.Plugins.Identity;

namespace Innovayse.SDK.Plugins.Fulfilment;
/// <summary>
/// Handed to <see cref="IFulfilmentPlugin.OnSubscriptionLapsedAsync"/> when a renewal invoice
/// stayed unpaid past every line's grace period. Nothing is deleted by the host; the plugin
/// decides what a lapsed subscription means for the product it fulfils.
/// </summary>
/// <param name="InvoiceId">The renewal invoice that lapsed.</param>
/// <param name="ClientId">The host's client id.</param>
/// <param name="Account">The client's external account, or null.</param>
/// <param name="Lines">Every product line on the invoice.</param>
/// <param name="DueDate">When the invoice was due (UTC).</param>
/// <param name="LapsedAt">When the host declared the lapse (UTC).</param>
public sealed record SubscriptionLapsedContext(
    int InvoiceId,
    int ClientId,
    ExternalAccount? Account,
    IReadOnlyList<FulfilmentLine> Lines,
    DateTimeOffset DueDate,
    DateTimeOffset LapsedAt);
