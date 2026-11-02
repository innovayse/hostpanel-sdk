using Innovayse.SDK.Plugins.Identity;

namespace Innovayse.SDK.Plugins.Fulfilment;

/// <summary>
/// Handed to <see cref="IFulfilmentPlugin.OnSubscriptionLapsedAsync"/> when a renewal invoice
/// stayed unpaid past every line's grace period. Nothing is deleted by the host; the plugin
/// decides what a lapsed subscription means for the product it fulfils.
/// </summary>
/// <param name="InvoiceId">The renewal invoice that lapsed.</param>
/// <param name="ClientId">The host's client id.</param>
/// <param name="ClientRef">
/// The plugin's own reference to the client: the same opaque value an endpoint call of this plugin for this client
/// carries in <see cref="Endpoints.PluginCaller.ClientRef"/>, so what a plugin sold can be joined with what its endpoints
/// see. It differs between plugins and cannot be turned back into <paramref name="ClientId"/>.
/// </param>
/// <param name="Account">The client's external account, or null.</param>
/// <param name="Lines">Every product line on the invoice.</param>
/// <param name="DueDate">When the invoice was due (UTC).</param>
/// <param name="LapsedAt">When the host declared the lapse (UTC).</param>
public sealed record SubscriptionLapsedContext(
    int InvoiceId,
    int ClientId,
    string ClientRef,
    ExternalAccount? Account,
    IReadOnlyList<FulfilmentLine> Lines,
    DateTimeOffset DueDate,
    DateTimeOffset LapsedAt);
