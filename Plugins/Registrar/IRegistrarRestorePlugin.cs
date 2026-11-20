using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The restore of an expired domain from its redemption period, as a separate opt in: a registrar plugin implements it next
/// to <see cref="IRegistrarOperationsPlugin"/> when its registrar offers the restore, and lists
/// <see cref="PluginCapabilities.RegistrarRestore"/> in its manifest. A host treats a plugin that does not implement it as
/// <see cref="PanelOperationOutcome.NotSupported"/>. It is a separate interface because a new operation always arrives as a
/// new interface, never as a member of an existing one.
/// </summary>
public interface IRegistrarRestorePlugin : IRegistrarOperationsPlugin
{
    /// <summary>
    /// Restores a domain that is in its redemption period, which the registrar usually charges for. Idempotent: a domain that
    /// is already restored and active answers <c>AlreadyApplied</c> with the same <see cref="RegistrarOrder.RegistrarRef"/>,
    /// and is never charged twice.
    /// </summary>
    /// <param name="context">The registrar account and the domain.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the order (<see cref="RegistrarOrder.RequiresPolling"/> when the registry has not finished), <c>AlreadyApplied</c>, <c>NotFound</c> for a domain the registrar does not hold, <c>NotSupported</c> or <c>Refused</c> (for example with <see cref="RegistrarRefusalReason.InsufficientFunds"/>).</returns>
    Task<ValueResult<RegistrarOrder, RegistrarRefusalReason>> RestoreDomainAsync(RegistrarDomainContext context, CancellationToken ct);
}
