using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Tax;

/// <summary>
/// What a tax service answered to <see cref="ITaxPlugin.CalculateTaxAsync"/>. Build it with <see cref="Calculated"/>,
/// <see cref="Refused"/> or <see cref="Unavailable"/>. There is no way to answer a line with a guessed rate: either
/// every line is calculated, or the whole calculation is refused or unavailable, and neither is ever zero tax. The
/// result carries no text of the service and no address or tax number.
/// </summary>
/// <param name="Outcome">How the service answered.</param>
/// <param name="Lines">
/// One entry per requested line; set exactly when <paramref name="Outcome"/> is <see cref="TaxOutcome.Calculated"/>.
/// </param>
/// <param name="Reason">
/// Why it refused; set exactly when <paramref name="Outcome"/> is <see cref="TaxOutcome.Refused"/>.
/// </param>
public sealed record TaxCalculationResult(
    TaxOutcome Outcome,
    IReadOnlyList<TaxLineResult>? Lines,
    TaxRefusalReason? Reason)
{
    /// <summary>
    /// Gets the tax of the whole invoice, the sum of the line amounts, or <see langword="null"/> when nothing was
    /// calculated. The constructor is public and checks nothing, as for the other results, so a result built by hand
    /// may hold a <see langword="null"/> entry in <see cref="Lines"/>; it adds nothing to the sum, so printing the result
    /// never throws. <see cref="Calculated"/> refuses such a list and the contract helper rejects it.
    /// </summary>
    public decimal? Total => Lines?.OfType<TaxLineResult>().Sum(line => line.Amount);

    /// <summary>Builds the answer for an invoice whose every line was calculated.</summary>
    /// <param name="lines">One result per requested line, in any order; empty for an invoice without lines.</param>
    /// <returns>A <see cref="TaxOutcome.Calculated"/> result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lines"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="lines"/> holds a <see langword="null"/> entry.</exception>
    public static TaxCalculationResult Calculated(IReadOnlyList<TaxLineResult> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Any(line => line is null))
        {
            throw new ArgumentException("A calculation never holds a null line.", nameof(lines));
        }

        return new TaxCalculationResult(TaxOutcome.Calculated, lines, null);
    }

    /// <summary>Builds the answer for a service that declined to calculate.</summary>
    /// <param name="reason">Why, in the host's terms.</param>
    /// <returns>A <see cref="TaxOutcome.Refused"/> result.</returns>
    public static TaxCalculationResult Refused(TaxRefusalReason reason)
    {
        return new TaxCalculationResult(TaxOutcome.Refused, null, reason);
    }

    /// <summary>Builds the answer for a service that could not answer now.</summary>
    /// <returns>An <see cref="TaxOutcome.Unavailable"/> result.</returns>
    public static TaxCalculationResult Unavailable()
    {
        return new TaxCalculationResult(TaxOutcome.Unavailable, null, null);
    }
}
