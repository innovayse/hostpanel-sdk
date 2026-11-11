using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Results;

/// <summary>
/// Result of an operation that answers a value: a registered domain, a created virtual server, a console session, a
/// listing. Build it with <see cref="Done"/>, <see cref="AlreadyDone"/>, <see cref="NotFound"/>,
/// <see cref="NotSupported"/> or <see cref="Refused"/>. It carries no text of the other side and no secret of its own: a
/// value that holds a secret prints it masked, and the result prints the value through its own <c>ToString</c>. A
/// <see cref="PanelOperationOutcome.Succeeded"/> or <see cref="PanelOperationOutcome.AlreadyApplied"/> result always has a
/// value; the factories refuse to build one without it.
/// </summary>
/// <typeparam name="TValue">The value the operation answers.</typeparam>
/// <typeparam name="TReason">The refusal reason enum of the contract. Its values start at 1.</typeparam>
/// <param name="Outcome">How the other side answered.</param>
/// <param name="Value">The value; set exactly when <paramref name="Outcome"/> is <see cref="PanelOperationOutcome.Succeeded"/> or <see cref="PanelOperationOutcome.AlreadyApplied"/>.</param>
/// <param name="Reason">Why it refused; set exactly when <paramref name="Outcome"/> is <see cref="PanelOperationOutcome.Refused"/>.</param>
public sealed record ValueResult<TValue, TReason>(PanelOperationOutcome Outcome, TValue? Value, TReason? Reason)
    : IOperationAnswer<TReason>, IValueAnswer
    where TValue : class
    where TReason : struct, Enum
{
    /// <inheritdoc />
    object? IValueAnswer.Value => Value;

    /// <summary>Builds the answer for an operation carried out now.</summary>
    /// <param name="value">The value the operation answers.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Succeeded"/> result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static ValueResult<TValue, TReason> Done(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ValueResult<TValue, TReason>(PanelOperationOutcome.Succeeded, value, null);
    }

    /// <summary>Builds the answer for an operation whose wanted state was already reached, for example a create that finds the thing an earlier attempt created.</summary>
    /// <param name="value">The value the operation answers; the same one the first call answered.</param>
    /// <returns>An <see cref="PanelOperationOutcome.AlreadyApplied"/> result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static ValueResult<TValue, TReason> AlreadyDone(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new ValueResult<TValue, TReason>(PanelOperationOutcome.AlreadyApplied, value, null);
    }

    /// <summary>Builds the answer for a target the other side does not hold.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotFound"/> result.</returns>
    public static ValueResult<TValue, TReason> NotFound()
    {
        return new ValueResult<TValue, TReason>(PanelOperationOutcome.NotFound, null, null);
    }

    /// <summary>Builds the answer for an operation the other side does not have.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotSupported"/> result.</returns>
    public static ValueResult<TValue, TReason> NotSupported()
    {
        return new ValueResult<TValue, TReason>(PanelOperationOutcome.NotSupported, null, null);
    }

    /// <summary>Builds the answer for an operation the other side refused.</summary>
    /// <param name="reason">Why, in the host's terms.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Refused"/> result.</returns>
    public static ValueResult<TValue, TReason> Refused(TReason reason)
    {
        return new ValueResult<TValue, TReason>(PanelOperationOutcome.Refused, null, reason);
    }
}
