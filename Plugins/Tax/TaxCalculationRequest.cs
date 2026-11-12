namespace Hostpanel.Sdk.Plugins.Tax;

/// <summary>
/// Input for <see cref="ITaxPlugin.CalculateTaxAsync"/>: the invoice as a tax service needs it to tell what tax is due
/// on each line. It holds personal data (the addresses and the tax numbers) and prints it masked.
/// </summary>
/// <param name="Currency">The ISO 4217 code of the currency every amount is in.</param>
/// <param name="PricesIncludeTax">
/// <see langword="true"/> when the line amounts already include tax, so the tax is taken out of them;
/// <see langword="false"/> when it is added on top.
/// </param>
/// <param name="Seller">The party that sells, with the business address the sale is made from.</param>
/// <param name="Buyer">The party that buys, with the address the supply is taxed at.</param>
/// <param name="Lines">The lines to calculate. May be empty, which is answered with no lines.</param>
public sealed record TaxCalculationRequest(
    string Currency,
    bool PricesIncludeTax,
    TaxParty Seller,
    TaxParty Buyer,
    IReadOnlyList<TaxLine> Lines);
