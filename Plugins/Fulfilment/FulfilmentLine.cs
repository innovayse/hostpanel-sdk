namespace Innovayse.SDK.Plugins.Fulfilment;

/// <summary>One product line of an invoice, as a fulfilment plugin sees it.</summary>
/// <param name="ProductId">The host's product id.</param>
/// <param name="Sku">
/// The product's stable machine identifier, or null when the product has none. Plugins route on it.
/// </param>
/// <param name="Metadata">The product's free-form JSON metadata, or null.</param>
/// <param name="Quantity">Units on the line (≥ 1).</param>
/// <param name="Amount">Line total in <paramref name="Currency"/>.</param>
/// <param name="Currency">ISO 4217 alpha code of the invoice.</param>
/// <param name="BillingCycle">"monthly", "annual", or null for a one-off line.</param>
public sealed record FulfilmentLine(
    int ProductId,
    string? Sku,
    string? Metadata,
    int Quantity,
    decimal Amount,
    string Currency,
    string? BillingCycle);
