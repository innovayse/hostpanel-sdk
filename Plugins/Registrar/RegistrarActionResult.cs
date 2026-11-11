using Hostpanel.Sdk.Plugins.Diagnostics;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// Result of a registrar operation that changes something and answers nothing else: a transfer cancelled, a nameserver
/// list, a lock, a privacy or auto renewal switch, a contact, a DNS record. Build it with <see cref="Done"/>,
/// <see cref="AlreadyDone"/>, <see cref="NotFound"/>, <see cref="NotSupported"/> or <see cref="Refused"/>. It carries no
/// text of the registrar and no secret.
/// </summary>
/// <param name="Outcome">How the registrar answered.</param>
/// <param name="Reason">Why the registrar refused; set exactly when <paramref name="Outcome"/> is <see cref="PanelOperationOutcome.Refused"/>.</param>
public sealed record RegistrarActionResult(PanelOperationOutcome Outcome, RegistrarRefusalReason? Reason)
    : IOperationAnswer<RegistrarRefusalReason>
{
    /// <summary>Builds the answer for a change the registrar made now.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.Succeeded"/> result.</returns>
    public static RegistrarActionResult Done()
    {
        return new RegistrarActionResult(PanelOperationOutcome.Succeeded, null);
    }

    /// <summary>Builds the answer for a change whose wanted state the domain was already in.</summary>
    /// <returns>An <see cref="PanelOperationOutcome.AlreadyApplied"/> result.</returns>
    public static RegistrarActionResult AlreadyDone()
    {
        return new RegistrarActionResult(PanelOperationOutcome.AlreadyApplied, null);
    }

    /// <summary>Builds the answer for a domain or record the registrar does not hold.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotFound"/> result.</returns>
    public static RegistrarActionResult NotFound()
    {
        return new RegistrarActionResult(PanelOperationOutcome.NotFound, null);
    }

    /// <summary>Builds the answer for an operation the registrar does not have.</summary>
    /// <returns>A <see cref="PanelOperationOutcome.NotSupported"/> result.</returns>
    public static RegistrarActionResult NotSupported()
    {
        return new RegistrarActionResult(PanelOperationOutcome.NotSupported, null);
    }

    /// <summary>Builds the answer for a change the registrar refused.</summary>
    /// <param name="reason">Why, in the host's terms.</param>
    /// <returns>A <see cref="PanelOperationOutcome.Refused"/> result.</returns>
    public static RegistrarActionResult Refused(RegistrarRefusalReason reason)
    {
        return new RegistrarActionResult(PanelOperationOutcome.Refused, reason);
    }
}
