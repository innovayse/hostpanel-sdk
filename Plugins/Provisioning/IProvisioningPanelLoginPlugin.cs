using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// A hosting control panel that can tell the host how a client signs in to an account. It extends
/// <see cref="IProvisioningPanelPlugin"/>, which does not change, so a panel plugin built on an earlier version loads and
/// runs as before and simply has no login details. The host checks <c>plugin is IProvisioningPanelLoginPlugin</c> at run
/// time and the manifest lists <see cref="PluginCapabilities.ProvisioningPanelLogin"/>. A plugin that does not implement
/// the interface is treated as <see cref="PanelOperationOutcome.NotSupported"/>.
/// </summary>
/// <remarks>
/// The rules of <see cref="IProvisioningPanelPlugin"/> hold here: the server arrives with its stored credentials, the
/// operation is a read and safe to repeat, a refusal and a missing account are outcomes and never exceptions, the panel's
/// own text is never returned, and the password of the details is a secret that no log line, result or exception message of
/// the plugin repeats.
/// </remarks>
public interface IProvisioningPanelLoginPlugin : IProvisioningPanelPlugin
{
    /// <summary>
    /// Reads how a client signs in to the account. Read only; safe to repeat, so there is no
    /// <see cref="PanelOperationOutcome.AlreadyApplied"/>.
    /// </summary>
    /// <param name="context">The server and the account.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>Succeeded</c> with the details, <c>NotFound</c> for an account the panel does not hold, <c>NotSupported</c> for a
    /// panel that has no sign-in page to show, or <c>Refused</c>.
    /// </returns>
    Task<ValueResult<PanelLoginDetails, PanelRefusalReason>> GetLoginDetailsAsync(PanelAccountContext context, CancellationToken ct);
}
