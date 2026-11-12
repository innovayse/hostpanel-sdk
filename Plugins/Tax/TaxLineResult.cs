namespace Hostpanel.Sdk.Plugins.Tax;

/// <summary>The tax due on one line.</summary>
/// <param name="LineId">The <see cref="TaxLine.LineId"/> of the line this answers.</param>
/// <param name="Rate">
/// The combined rate as a percentage between 0 and 100: 20 means 20 per cent. 0 is a real rate (an exempt or reverse
/// charged supply), never the answer for a rate the service does not know.
/// </param>
/// <param name="Amount">
/// The tax in the currency of the request, rounded the way the service rounds, never negative. With
/// <paramref name="Rate"/> it follows the line: the line total times the rate when prices exclude tax, and the part of
/// the line total that is tax at that rate when they include it.
/// </param>
/// <param name="Jurisdiction">Where the tax is due.</param>
public sealed record TaxLineResult(string LineId, decimal Rate, decimal Amount, TaxJurisdiction Jurisdiction);
