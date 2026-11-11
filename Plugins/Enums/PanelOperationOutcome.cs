namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>
/// How the other side answered one operation of <see cref="Provisioning.IProvisioningPanelPlugin"/>, of
/// <see cref="VirtualServers.IVirtualServerPlugin"/> or of <see cref="Registrar.IRegistrarOperationsPlugin"/>: a hosting
/// panel, a virtual server platform or a registrar. The values start at 1
/// and 0 is not valid: a default-initialised value means the plugin set nothing, and the host refuses it. A refusal, a
/// missing account and an operation the panel does not have are outcomes, never exceptions: a plugin throws only when it
/// does not know what happened, for example when the panel could not be reached.
/// </summary>
public enum PanelOperationOutcome
{
    /// <summary>The panel did what was asked now.</summary>
    Succeeded = 1,

    /// <summary>
    /// The panel was already in the wanted state, typically because an earlier attempt did the work and its answer was
    /// lost: the account already exists, is already suspended or active, is already gone, or is already on the package.
    /// It is a success for the host, and it is the answer of every retry after the first.
    /// </summary>
    AlreadyApplied = 2,

    /// <summary>The panel holds no account under the reference the host sent. Never the answer to a terminate: a missing account is already terminated.</summary>
    NotFound = 3,

    /// <summary>The panel refused. The result carries a <see cref="PanelRefusalReason"/>; the panel's own text is never repeated.</summary>
    Refused = 4,

    /// <summary>The panel has no such operation, for example no usage figures or no single sign on. The host stops offering the feature; it is not a failure.</summary>
    NotSupported = 5,
}
