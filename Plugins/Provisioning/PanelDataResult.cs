using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Result of an operation that answers a value: a virtual server that was created, a console session, the figures of a
/// server or a catalogue. Build it with <see cref="Done"/>, <see cref="AlreadyDone"/>, <see cref="NotFound"/>,
/// <see cref="NotSupported"/> or <see cref="Refused"/>. It carries no text of the other side and no secret of its own: a
/// value that holds a secret prints it masked, and the result prints the value through its own <c>ToString</c>.
/// </summary>
/// <typeparam name="T">The value the operation answers.</typeparam>
/// <param name="Outcome">How the other side answered.</param>
/// <param name="Value">The value; set exactly when <paramref name="Outcome"/> is <see cref="PanelOperationOutcome.Succeeded"/> or <see cref="PanelOperationOutcome.AlreadyApplied"/>.</param>
/// <param name="Reason">Why it refused; set exactly when <paramref name="Outcome"/> is <see cref="PanelOperationOutcome.Refused"/>.</param>
public sealed record PanelDataResult<T>(PanelOperationOutcome Outcome, T? Value, PanelRefusalReason? Reason)
    : IOperationAnswer<PanelRefusalReason>
    where T : class
{
    /// <summary>Builds the answer for an operation carried out now.</summary>
    /// <param name="value">The value the operation answers.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Succeeded"/> result.</returns>
    public static PanelDataResult<T> Done(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new PanelDataResult<T>(PanelOperationOutcome.Succeeded, value, null);
    }

    /// <summary>Builds the answer for an operation whose wanted state was already reached, for example a create that finds the server an earlier attempt created.</summary>
    /// <param name="value">The value the operation answers; the same one the first call answered.</param>
    /// <returns>An <see cref="PanelOperationOutcome.AlreadyApplied"/> result.</returns>
    public static PanelDataResult<T> AlreadyDone(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new PanelDataResult<T>(PanelOperationOutcome.AlreadyApplied, value, null);
    }

    /// <summary>Builds the answer for a target the other side does not hold.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotFound"/> result.</returns>
    public static PanelDataResult<T> NotFound()
    {
        return new PanelDataResult<T>(PanelOperationOutcome.NotFound, null, null);
    }

    /// <summary>Builds the answer for an operation the other side does not have.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotSupported"/> result.</returns>
    public static PanelDataResult<T> NotSupported()
    {
        return new PanelDataResult<T>(PanelOperationOutcome.NotSupported, null, null);
    }

    /// <summary>Builds the answer for an operation the other side refused.</summary>
    /// <param name="reason">Why, in the host's terms.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Refused"/> result.</returns>
    public static PanelDataResult<T> Refused(PanelRefusalReason reason)
    {
        return new PanelDataResult<T>(PanelOperationOutcome.Refused, null, reason);
    }
}
