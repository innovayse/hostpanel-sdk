using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Identity;

namespace Hostpanel.Sdk.Plugins.Fulfilment;

/// <summary>Handed to <see cref="IFulfilmentPlugin.OnInvoiceRefundedAsync"/> after the money moved back.</summary>
/// <param name="InvoiceId">The refunded invoice.</param>
/// <param name="ClientId">The host's client id.</param>
/// <param name="ClientRef">
/// The plugin's own reference to the client: the same opaque value an endpoint call of this plugin for this client
/// carries in <see cref="Endpoints.PluginCaller.ClientRef"/>, so what a plugin sold can be joined with what its endpoints
/// see. It differs between plugins and cannot be turned back into <paramref name="ClientId"/>.
/// </param>
/// <param name="Account">The client's external account, or null.</param>
/// <param name="Lines">Every product line on the invoice.</param>
/// <param name="RefundedAmount">What was refunded, in <paramref name="Currency"/>.</param>
/// <param name="Currency">ISO 4217 alpha code of the invoice.</param>
/// <param name="TransactionReference">The refund's gateway reference, or null.</param>
/// <param name="RefundedAt">When the refund was recorded (UTC).</param>
public sealed partial record InvoiceRefundedContext(
    int InvoiceId,
    int ClientId,
    [property: SecretMember] string ClientRef,
    ExternalAccount? Account,
    IReadOnlyList<FulfilmentLine> Lines,
    decimal RefundedAmount,
    string Currency,
    string? TransactionReference,
    DateTimeOffset RefundedAt);
