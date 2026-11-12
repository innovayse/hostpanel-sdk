namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// Why a tax service refused to calculate, in the host's terms. The host turns the value into a sentence in the
/// caller's language, so a plugin never returns the service's own text, an address or a tax number to describe a
/// refusal. The values start at 1 and 0 is not valid. New values are only ever appended.
/// </summary>
public enum TaxRefusalReason
{
    /// <summary>
    /// The service cannot calculate tax for an address: the country or region is not covered, or the address is too
    /// incomplete to place.
    /// </summary>
    AddressUnsupported = 1,

    /// <summary>The buyer's tax number was rejected as malformed or not registered.</summary>
    TaxIdInvalid = 2,

    /// <summary>The service does not calculate in the currency of the request.</summary>
    CurrencyNotSupported = 3,

    /// <summary>A line's tax category is not one the service knows.</summary>
    CategoryUnknown = 4,

    /// <summary>The credentials of the plugin are valid but not allowed to calculate.</summary>
    AccessDenied = 5,

    /// <summary>The service refused for a reason none of the other values describes.</summary>
    Other = 6,
}
