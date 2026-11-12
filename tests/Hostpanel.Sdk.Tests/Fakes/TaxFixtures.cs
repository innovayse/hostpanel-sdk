using Hostpanel.Sdk.Plugins.Tax;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>The requests and the answers the tax tests share.</summary>
internal static class TaxFixtures
{
    /// <summary>The buyer's tax number, which no printed form may show.</summary>
    internal const string BuyerTaxId = "FR12345678901";

    /// <summary>The buyer's street, which no printed form may show.</summary>
    internal const string BuyerStreet = "12 Example Street";

    /// <summary>The rate, as a percentage, the well behaved fake applies.</summary>
    internal const decimal Rate = 20m;

    /// <summary>An invoice of two lines, the second a digital service, with prices that exclude tax.</summary>
    internal static readonly TaxCalculationRequest Request = new(
        "EUR",
        false,
        new TaxParty(new TaxAddress("DE", "BE", "10115", "Berlin", "1 Seller Street"), true, "DE999999999"),
        new TaxParty(new TaxAddress("FR", null, "75001", "Paris", BuyerStreet), false, BuyerTaxId),
        [new TaxLine("l1", 100.00m, 1, null, false), new TaxLine("l2", 50.00m, 2, "software", true)]);

    /// <summary>The same invoice with prices that include tax.</summary>
    internal static readonly TaxCalculationRequest InclusiveRequest = Request with { PricesIncludeTax = true };

    /// <summary>The jurisdiction the well behaved fake answers.</summary>
    internal static readonly TaxJurisdiction France = new("FR", null);

    /// <summary>Builds the correct calculation of a request at <see cref="Rate"/>.</summary>
    /// <param name="request">The request to calculate.</param>
    /// <returns>One line per requested line, with the exact tax for the request's price mode.</returns>
    internal static TaxCalculationResult Correct(TaxCalculationRequest request)
    {
        var lines = request.Lines.Select(line => new TaxLineResult(
            line.LineId,
            Rate,
            Math.Round(request.PricesIncludeTax ? line.Amount * Rate / (100m + Rate) : line.Amount * Rate / 100m, 2),
            France)).ToList();
        return TaxCalculationResult.Calculated(lines);
    }
}
