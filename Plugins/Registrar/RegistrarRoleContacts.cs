namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The contacts a domain has besides the registrant, one per role. Many registries keep an administrative, a technical and a
/// billing contact next to the registrant; a role the host has no contact for is <see langword="null"/>, and the registrar
/// then gets the registrant for it. A plugin never branches on <see langword="null"/> itself: it asks
/// <see cref="AdminOr"/>, <see cref="TechnicalOr"/> or <see cref="BillingOr"/>, which hold the fallback in one place. Every
/// member is a <see cref="RegistrarContact"/>, which prints masked, so this record leaks nothing either.
/// </summary>
/// <param name="Admin">The administrative contact, or <see langword="null"/> to use the registrant.</param>
/// <param name="Technical">The technical contact, or <see langword="null"/> to use the registrant.</param>
/// <param name="Billing">The billing contact, or <see langword="null"/> to use the registrant.</param>
public sealed record RegistrarRoleContacts(RegistrarContact? Admin, RegistrarContact? Technical, RegistrarContact? Billing)
{
    /// <summary>Gets the value that holds no role contact at all, so every role falls back to the registrant. It is what a context carries by default.</summary>
    public static RegistrarRoleContacts None { get; } = new(null, null, null);

    /// <summary>Gets the administrative contact, falling back to the registrant.</summary>
    /// <param name="registrant">The registrant contact of the same context.</param>
    /// <returns><see cref="Admin"/> when the host sent one, otherwise <paramref name="registrant"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="registrant"/> is <see langword="null"/>.</exception>
    public RegistrarContact AdminOr(RegistrarContact registrant)
    {
        ArgumentNullException.ThrowIfNull(registrant);

        return Admin ?? registrant;
    }

    /// <summary>Gets the technical contact, falling back to the registrant.</summary>
    /// <param name="registrant">The registrant contact of the same context.</param>
    /// <returns><see cref="Technical"/> when the host sent one, otherwise <paramref name="registrant"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="registrant"/> is <see langword="null"/>.</exception>
    public RegistrarContact TechnicalOr(RegistrarContact registrant)
    {
        ArgumentNullException.ThrowIfNull(registrant);

        return Technical ?? registrant;
    }

    /// <summary>Gets the billing contact, falling back to the registrant.</summary>
    /// <param name="registrant">The registrant contact of the same context.</param>
    /// <returns><see cref="Billing"/> when the host sent one, otherwise <paramref name="registrant"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="registrant"/> is <see langword="null"/>.</exception>
    public RegistrarContact BillingOr(RegistrarContact registrant)
    {
        ArgumentNullException.ThrowIfNull(registrant);

        return Billing ?? registrant;
    }
}
