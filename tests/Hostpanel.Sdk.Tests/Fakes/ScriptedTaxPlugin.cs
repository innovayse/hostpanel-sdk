using Hostpanel.Sdk.Plugins.Tax;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// A tax plugin whose <c>CalculateTaxAsync</c> is scripted by the test, so the contract helper can be shown to accept a
/// well behaved plugin and to reject each way of breaking a promise.
/// </summary>
internal sealed class ScriptedTaxPlugin : ITaxPlugin
{
    /// <summary>
    /// What <c>CalculateTaxAsync</c> does for a request and a call number, which starts at 1; it may throw.
    /// </summary>
    private readonly Func<TaxCalculationRequest, int, TaxCalculationResult?> _calculate;

    /// <summary>Initializes a new instance of the <see cref="ScriptedTaxPlugin"/> class.</summary>
    /// <param name="calculate">
    /// The answer for a request and a call number, a <see langword="null"/> answer, or a throw.
    /// </param>
    public ScriptedTaxPlugin(Func<TaxCalculationRequest, int, TaxCalculationResult?> calculate)
    {
        _calculate = calculate;
    }

    /// <summary>Gets how many times <c>CalculateTaxAsync</c> was called.</summary>
    public int Calls { get; private set; }

    /// <inheritdoc />
    public Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request, CancellationToken ct)
    {
        Calls++;
        return Task.FromResult(_calculate(request, Calls)!);
    }
}
