using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The DNS management switch of a registrar, as a separate opt in: a registrar plugin implements it next to
/// <see cref="IRegistrarOperationsPlugin"/> when its registrar lets the host turn DNS hosting of a domain on and off, and
/// lists <see cref="PluginCapabilities.RegistrarDnsManagement"/> in its manifest. A host treats a plugin that does not
/// implement it as <see cref="PanelOperationOutcome.NotSupported"/>. The records themselves are read and changed through
/// <see cref="IRegistrarOperationsPlugin"/>. It is a separate interface because a new operation always arrives as a new
/// interface, never as a member of an existing one.
/// </summary>
public interface IRegistrarDnsManagementPlugin : IRegistrarOperationsPlugin
{
    /// <summary>Switches DNS management of a domain on or off. Idempotent: a domain already in the wanted state is a success.</summary>
    /// <param name="context">The registrar account, the domain and the wanted state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c>, <c>AlreadyApplied</c>, <c>NotFound</c> for a domain the registrar does not hold, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<OperationResult<RegistrarRefusalReason>> SetDnsManagementAsync(RegistrarFlagContext context, CancellationToken ct);
}
