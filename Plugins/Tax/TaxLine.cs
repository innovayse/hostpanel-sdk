namespace Hostpanel.Sdk.Plugins.Tax;

/// <summary>One line of an invoice, as a tax service sees it.</summary>
/// <param name="LineId">
/// The host's identifier of the line. It is unique within the request and the result answers it back unchanged.
/// </param>
/// <param name="Amount">
/// The line total in the currency of the request, with every discount already taken off, never negative. It includes
/// tax when <see cref="TaxCalculationRequest.PricesIncludeTax"/> is <see langword="true"/>.
/// </param>
/// <param name="Quantity">The units on the line; at least 1. The amount is already the total of all of them.</param>
/// <param name="TaxCategory">
/// The product's tax category code as the host stores it on the product, or <see langword="null"/> for the standard
/// category. The plugin maps it to its service's own category.
/// </param>
/// <param name="IsDigitalService">
/// <see langword="true"/> for a digital or electronically supplied service, which many jurisdictions tax at the buyer's
/// place and not the seller's.
/// </param>
public sealed record TaxLine(string LineId, decimal Amount, int Quantity, string? TaxCategory, bool IsDigitalService);
