namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// What changed on a service, as a fulfilment plugin sees it (<see cref="Fulfilment.ServicePlanChangedContext.Kind"/>).
/// Values start at 1; 0 is not a kind. A newer host may add a kind this SDK version does not name; a plugin checks
/// <see cref="System.Enum.IsDefined(System.Type, object)"/> and, for an unknown kind, compares the previous and the new line
/// itself instead of throwing.
/// </summary>
public enum ServicePlanChangeKind
{
    /// <summary>The service moved to a higher plan.</summary>
    Upgrade = 1,

    /// <summary>The service moved to a lower plan.</summary>
    Downgrade = 2,

    /// <summary>The plan is the same and the number of units changed.</summary>
    QuantityChange = 3,
}
