using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Result of a registrar operation that answers a value: an order, an availability, a contact, a list of records. Build it
/// with <see cref="Done"/>, <see cref="AlreadyDone"/>, <see cref="NotFound"/>, <see cref="NotSupported"/> or
/// <see cref="Refused"/>. It carries no text of the registrar and no secret of its own: a value that holds a secret prints
/// it masked, and the result prints the value through its own <c>ToString</c>.
/// </summary>
/// <typeparam name="T">The value the operation answers.</typeparam>
/// <param name="Outcome">How the registrar answered.</param>
/// <param name="Value">The value; set exactly when <paramref name="Outcome"/> is <see cref="PanelOperationOutcome.Succeeded"/> or <see cref="PanelOperationOutcome.AlreadyApplied"/>.</param>
/// <param name="Reason">Why the registrar refused; set exactly when <paramref name="Outcome"/> is <see cref="PanelOperationOutcome.Refused"/>.</param>
public sealed record RegistrarDataResult<T>(PanelOperationOutcome Outcome, T? Value, RegistrarRefusalReason? Reason)
    : IOperationAnswer<RegistrarRefusalReason>
    where T : class
{
    /// <summary>Builds the answer for an operation the registrar carried out now.</summary>
    /// <param name="value">The value the operation answers.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Succeeded"/> result.</returns>
    public static RegistrarDataResult<T> Done(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new RegistrarDataResult<T>(PanelOperationOutcome.Succeeded, value, null);
    }

    /// <summary>Builds the answer for an operation whose wanted state was already reached, for example a registration that finds the domain an earlier attempt registered.</summary>
    /// <param name="value">The value the operation answers; the same one the first call answered.</param>
    /// <returns>An <see cref="PanelOperationOutcome.AlreadyApplied"/> result.</returns>
    public static RegistrarDataResult<T> AlreadyDone(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new RegistrarDataResult<T>(PanelOperationOutcome.AlreadyApplied, value, null);
    }

    /// <summary>Builds the answer for a domain the registrar does not hold.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotFound"/> result.</returns>
    public static RegistrarDataResult<T> NotFound()
    {
        return new RegistrarDataResult<T>(PanelOperationOutcome.NotFound, null, null);
    }

    /// <summary>Builds the answer for an operation the registrar does not have.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotSupported"/> result.</returns>
    public static RegistrarDataResult<T> NotSupported()
    {
        return new RegistrarDataResult<T>(PanelOperationOutcome.NotSupported, null, null);
    }

    /// <summary>Builds the answer for an operation the registrar refused.</summary>
    /// <param name="reason">Why, in the host's terms.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Refused"/> result.</returns>
    public static RegistrarDataResult<T> Refused(RegistrarRefusalReason reason)
    {
        return new RegistrarDataResult<T>(PanelOperationOutcome.Refused, null, reason);
    }
}
