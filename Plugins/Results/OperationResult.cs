using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Results;

/// <summary>
/// Result of an operation that changes something and answers nothing else, for a contract whose refusal reasons are
/// <typeparamref name="TReason"/>: the registrar operations use it with <see cref="RegistrarRefusalReason"/>. Build it with
/// <see cref="Done"/>, <see cref="AlreadyDone"/>, <see cref="NotFound"/>, <see cref="NotSupported"/> or
/// <see cref="Refused"/>. It carries no text of the other side and no secret. The older
/// <see cref="Provisioning.PanelActionResult"/> is the same shape for <see cref="PanelRefusalReason"/>; it stays as it is,
/// because changing its type would break plugins built on earlier versions.
/// </summary>
/// <typeparam name="TReason">The refusal reason enum of the contract. Its values start at 1.</typeparam>
/// <param name="Outcome">How the other side answered.</param>
/// <param name="Reason">Why it refused; set exactly when <paramref name="Outcome"/> is <see cref="PanelOperationOutcome.Refused"/>.</param>
public sealed record OperationResult<TReason>(PanelOperationOutcome Outcome, TReason? Reason)
    : IOperationAnswer<TReason>
    where TReason : struct, Enum
{
    /// <summary>Builds the answer for a change the other side made now.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.Succeeded"/> result.</returns>
    public static OperationResult<TReason> Done()
    {
        return new OperationResult<TReason>(PanelOperationOutcome.Succeeded, null);
    }

    /// <summary>Builds the answer for a change whose wanted state was already reached.</summary>
    /// <returns>An <see cref="PanelOperationOutcome.AlreadyApplied"/> result.</returns>
    public static OperationResult<TReason> AlreadyDone()
    {
        return new OperationResult<TReason>(PanelOperationOutcome.AlreadyApplied, null);
    }

    /// <summary>Builds the answer for a target the other side does not hold.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotFound"/> result.</returns>
    public static OperationResult<TReason> NotFound()
    {
        return new OperationResult<TReason>(PanelOperationOutcome.NotFound, null);
    }

    /// <summary>Builds the answer for an operation the other side does not have.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotSupported"/> result.</returns>
    public static OperationResult<TReason> NotSupported()
    {
        return new OperationResult<TReason>(PanelOperationOutcome.NotSupported, null);
    }

    /// <summary>Builds the answer for a change the other side refused.</summary>
    /// <param name="reason">Why, in the host's terms.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Refused"/> result.</returns>
    public static OperationResult<TReason> Refused(TReason reason)
    {
        return new OperationResult<TReason>(PanelOperationOutcome.Refused, reason);
    }
}
