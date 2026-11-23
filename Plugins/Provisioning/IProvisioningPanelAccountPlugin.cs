using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// A hosting control panel that can read one account by its reference, as <see cref="PanelAccount"/>. It extends
/// <see cref="IProvisioningPanelPlugin"/>, which does not change, so a panel plugin built on an earlier version loads and
/// runs as before and simply has no single account read. The host checks <c>plugin is IProvisioningPanelAccountPlugin</c> at
/// run time and the manifest lists <see cref="PluginCapabilities.ProvisioningPanelAccount"/>. A plugin that does not implement
/// the interface is treated as <see cref="PanelOperationOutcome.NotSupported"/>.
/// </summary>
/// <remarks>
/// The rules of <see cref="IProvisioningPanelPlugin"/> hold here: the server arrives with its stored credentials, the
/// operation is a read and safe to repeat, a refusal is an outcome and never an exception, and the panel's own text is
/// never returned.
/// </remarks>
public interface IProvisioningPanelAccountPlugin : IProvisioningPanelPlugin
{
    /// <summary>
    /// Reads one account. Read only; safe to repeat. The account carries the same <see cref="PanelAccount.ProvisioningRef"/>
    /// the context named, and a field the panel does not report is <see langword="null"/>.
    /// </summary>
    /// <param name="context">The server and the account to read; <see cref="PanelService.ProvisioningRef"/> is set.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the account, <c>NotFound</c> when the panel holds no such account, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<PanelAccount, PanelRefusalReason>> GetAccountAsync(PanelAccountContext context, CancellationToken ct);
}
