using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Whether a domain name can be registered now. <see cref="Reason"/> and <see cref="PremiumPrice"/> are optional members with
/// a default, so a plugin built on an earlier version, which answers only the flag, loads and answers as before, and a host
/// that reads them treats <see langword="null"/> as "the registrar did not say".
/// </summary>
/// <param name="IsAvailable"><see langword="true"/> when the registrar would accept a registration of the name; <see langword="false"/> when it is taken, reserved or not offered.</param>
public sealed record RegistrarAvailability(bool IsAvailable)
{
    /// <summary>
    /// Gets the reason the name cannot be registered, or what makes an available name special
    /// (<see cref="RegistrarAvailabilityReason.Premium"/>), or <see langword="null"/> when the registrar does not say.
    /// </summary>
    public RegistrarAvailabilityReason? Reason { get; init; }

    /// <summary>
    /// Gets the premium price of an available name, or <see langword="null"/> when the name costs the standard price of its
    /// top level domain or the registrar does not say.
    /// </summary>
    public RegistrarPremiumPrice? PremiumPrice { get; init; }
}
