using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// Result of an operation that changes an account and returns nothing else: suspend, unsuspend, terminate, change package
/// and change password. Build it with <see cref="Done"/>, <see cref="AlreadyDone"/>, <see cref="NotFound"/>,
/// <see cref="NotSupported"/> or <see cref="Refused"/>. It carries no panel text and no secret.
/// </summary>
/// <param name="Outcome">How the panel answered.</param>
/// <param name="Reason">Why the panel refused; set exactly when <paramref name="Outcome"/> is <see cref="PanelOperationOutcome.Refused"/>.</param>
public sealed record PanelActionResult(PanelOperationOutcome Outcome, PanelRefusalReason? Reason)
{
    /// <summary>Builds the answer for an operation the panel carried out now.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.Succeeded"/> result.</returns>
    public static PanelActionResult Done()
    {
        return new PanelActionResult(PanelOperationOutcome.Succeeded, null);
    }

    /// <summary>Builds the answer for an operation whose wanted state the panel was already in.</summary>
    /// <returns>An <see cref="PanelOperationOutcome.AlreadyApplied"/> result.</returns>
    public static PanelActionResult AlreadyDone()
    {
        return new PanelActionResult(PanelOperationOutcome.AlreadyApplied, null);
    }

    /// <summary>Builds the answer for an account the panel does not hold.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotFound"/> result.</returns>
    public static PanelActionResult NotFound()
    {
        return new PanelActionResult(PanelOperationOutcome.NotFound, null);
    }

    /// <summary>Builds the answer for an operation the panel does not have.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotSupported"/> result.</returns>
    public static PanelActionResult NotSupported()
    {
        return new PanelActionResult(PanelOperationOutcome.NotSupported, null);
    }

    /// <summary>Builds the answer for an operation the panel refused.</summary>
    /// <param name="reason">Why, in the host's terms.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Refused"/> result.</returns>
    public static PanelActionResult Refused(PanelRefusalReason reason)
    {
        return new PanelActionResult(PanelOperationOutcome.Refused, reason);
    }
}
