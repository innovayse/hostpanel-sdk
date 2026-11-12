namespace Hostpanel.Sdk.Plugins.Tax;

/// <summary>
/// Contract for a tax plugin (<see cref="Enums.PluginType.Tax"/>, or any plugin that lists the
/// <see cref="Manifest.PluginCapabilities.Tax"/> capability): it tells the host what tax is due on the lines of an
/// invoice, so the rate comes from a tax service instead of a number typed by hand. The calculation is a read: it
/// changes nothing at the service, so a repeat of the same request is safe and answers the same. The plugin keeps its
/// credentials and settings itself, like a payment plugin, and the host passes nothing but the request.
/// </summary>
public interface ITaxPlugin
{
    /// <summary>
    /// Calculates the tax of every line of an invoice. The rules the plugin keeps, and <see cref="TaxContract"/> checks
    /// in the plugin's own tests:
    /// <list type="bullet">
    /// <item><description>Every requested line is answered exactly once, by its <see cref="TaxLine.LineId"/>, and no
    /// other line is added.</description></item>
    /// <item><description>A rate is never guessed. A line the service cannot rate makes the whole calculation
    /// <see cref="Enums.TaxOutcome.Refused"/>, never a line with a default rate.</description></item>
    /// <item><description>A service that cannot be reached, times out or is rate limited is
    /// <see cref="Enums.TaxOutcome.Unavailable"/>, never a calculation of zero tax.</description></item>
    /// <item><description>A rate of 0 is a real answer (an exempt or reverse charged supply) and is given with an
    /// amount of 0.</description></item>
    /// <item><description>Amounts are never negative, the rate is a percentage between 0 and 100, and the amount
    /// follows the rate and the line total, with <see cref="TaxCalculationRequest.PricesIncludeTax"/> deciding whether
    /// the tax is added or taken out.</description></item>
    /// <item><description>A refusal and an unavailable service are outcomes, not exceptions. Throw only when the result
    /// is unknown and nothing can be said about it.</description></item>
    /// <item><description>The service's own text, an address or a tax number is never put in a result, a log line or an
    /// exception message.</description></item>
    /// </list>
    /// </summary>
    /// <param name="request">The invoice to calculate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The calculation, a refusal with its reason, or an unavailable service.</returns>
    Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request, CancellationToken ct);
}
