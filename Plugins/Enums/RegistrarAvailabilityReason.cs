namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// Why a domain name cannot be registered now, or what sets it apart when it can, as a registrar answers it in
/// <see cref="Registrar.RegistrarAvailability.Reason"/>. The values start at 1 and 0 is not valid. New values are only ever
/// appended.
/// </summary>
public enum RegistrarAvailabilityReason
{
    /// <summary>The name is already registered.</summary>
    Registered = 1,

    /// <summary>The name is reserved by the registry and not offered for registration.</summary>
    Reserved = 2,

    /// <summary>The registrar does not sell the top level domain of the name.</summary>
    TldNotSupported = 3,

    /// <summary>The name is not a valid domain name for its top level domain.</summary>
    InvalidName = 4,

    /// <summary>The name can be registered, but at a premium price: see <see cref="Registrar.RegistrarAvailability.PremiumPrice"/>.</summary>
    Premium = 5,

    /// <summary>The name cannot be registered for a reason none of the other values describes.</summary>
    Other = 6,
}
