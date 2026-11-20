using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The child nameserver hosts of a domain (the glue records of a nameserver that lives under the domain it serves), as a
/// separate opt in: a registrar plugin implements it next to <see cref="IRegistrarOperationsPlugin"/> when its registrar lets
/// the host register its own nameserver names, and lists <see cref="PluginCapabilities.RegistrarChildNameservers"/> in its
/// manifest. A host treats a plugin that does not implement it as <see cref="PanelOperationOutcome.NotSupported"/>. It is a
/// separate interface because a new operation always arrives as a new interface, never as a member of an existing one.
/// </summary>
public interface IRegistrarChildNameserversPlugin : IRegistrarOperationsPlugin
{
    /// <summary>Creates a nameserver host under a domain. Idempotent: a host that already exists with the same addresses is a success.</summary>
    /// <param name="context">The registrar account, the domain, the host name and its addresses.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> for a domain the registrar does not hold, <c>NotSupported</c> or <c>Refused</c> (for example with <see cref="RegistrarRefusalReason.NameserverInvalid"/>).</returns>
    Task<OperationResult<RegistrarRefusalReason>> CreateChildNameserverAsync(RegistrarChildNameserverContext context, CancellationToken ct);

    /// <summary>Replaces the addresses of a nameserver host. Idempotent: a host that already has exactly these addresses is a success.</summary>
    /// <param name="context">The registrar account, the domain, the host name and the addresses it should have.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> for a domain or a host the registrar does not hold, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<OperationResult<RegistrarRefusalReason>> UpdateChildNameserverAsync(RegistrarChildNameserverContext context, CancellationToken ct);

    /// <summary>Deletes a nameserver host. Idempotent: a host that is already gone is <c>AlreadyApplied</c>, never <c>NotFound</c>.</summary>
    /// <param name="context">The registrar account, the domain and the host name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> for a domain the registrar does not hold, <c>NotSupported</c> or <c>Refused</c> (for example while a domain still delegates to the host).</returns>
    Task<OperationResult<RegistrarRefusalReason>> DeleteChildNameserverAsync(RegistrarChildNameserverRemovalContext context, CancellationToken ct);
}
