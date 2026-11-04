namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// How often a product line renews, as a fulfilment plugin sees it (<see cref="Fulfilment.FulfilmentLine.BillingCycle"/>).
/// Values start at 1; 0 is not a cycle. A one-off line has no cycle at all and reports <see langword="null"/>.
/// </summary>
/// <remarks>
/// The host sends every cycle below: <c>Monthly</c> 1, <c>Annual</c> 2, <c>Quarterly</c> 3, <c>SemiAnnual</c> 4 and
/// <c>Biennial</c> 5. A newer host may add a cycle this SDK version does not name; the line then carries that number.
/// A plugin checks <see cref="System.Enum.IsDefined(System.Type, object)"/> and treats an unknown cycle as a renewing line
/// whose period it does not know (for example it grants what the line paid for without assuming a term), never as
/// one-off and never by throwing.
/// </remarks>
public enum PluginBillingCycle
{
    /// <summary>Every month.</summary>
    Monthly = 1,

    /// <summary>Every year.</summary>
    Annual = 2,

    /// <summary>Every three months.</summary>
    Quarterly = 3,

    /// <summary>Every six months.</summary>
    SemiAnnual = 4,

    /// <summary>Every two years.</summary>
    Biennial = 5,
}
