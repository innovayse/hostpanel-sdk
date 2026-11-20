using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The balance of the registrar account, as a separate opt in: a registrar plugin implements it next to
/// <see cref="IRegistrarOperationsPlugin"/> when its registrar is prepaid, and lists
/// <see cref="PluginCapabilities.RegistrarBalance"/> in its manifest. A host treats a plugin that does not implement it as
/// <see cref="PanelOperationOutcome.NotSupported"/>. It is a separate interface because a new operation always arrives as a
/// new interface, never as a member of an existing one.
/// </summary>
public interface IRegistrarBalancePlugin : IRegistrarOperationsPlugin
{
    /// <summary>Reads the balance of the registrar account. Read only; safe to repeat.</summary>
    /// <param name="context">The registrar account.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the balance, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<RegistrarBalance, RegistrarRefusalReason>> GetBalanceAsync(RegistrarBalanceContext context, CancellationToken ct);
}
