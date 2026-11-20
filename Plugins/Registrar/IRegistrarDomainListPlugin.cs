using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.Registrar;

/// <summary>
/// The list of the domains a registrar account holds, as a separate opt in for importing them: a registrar plugin implements
/// it next to <see cref="IRegistrarOperationsPlugin"/> and lists <see cref="PluginCapabilities.RegistrarDomainList"/> in its
/// manifest. A host treats a plugin that does not implement it as <see cref="PanelOperationOutcome.NotSupported"/>. It is a
/// separate interface because a new operation always arrives as a new interface, never as a member of an existing one.
/// </summary>
public interface IRegistrarDomainListPlugin : IRegistrarOperationsPlugin
{
    /// <summary>
    /// Lists the domains the account holds, one page per call. Read only; safe to repeat. Following
    /// <see cref="RegistrarDomainPage.NextCursor"/> to the end returns every domain exactly once.
    /// </summary>
    /// <param name="context">The registrar account and the cursor of the page wanted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with a page, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<RegistrarDomainPage, RegistrarRefusalReason>> ListDomainsAsync(RegistrarDomainListContext context, CancellationToken ct);
}
