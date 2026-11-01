namespace Innovayse.SDK.Plugins.Enums;

/// <summary>
/// How often a product line renews, as a fulfilment plugin sees it (<see cref="Fulfilment.FulfilmentLine.BillingCycle"/>).
/// Values start at 1; 0 is not a cycle. A one-off line has no cycle at all and reports <see langword="null"/>.
/// </summary>
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
