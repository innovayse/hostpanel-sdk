using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Results;

namespace Hostpanel.Sdk.Plugins.Provisioning;

/// <summary>
/// A hosting control panel that can list the packages a server offers, with the limits it knows. It extends
/// <see cref="IProvisioningPanelPlugin"/>, which does not change, so a panel plugin built on an earlier version loads and
/// runs as before and simply has no package listing. The host checks <c>plugin is IProvisioningPanelPackagesPlugin</c> at run
/// time and the manifest lists <see cref="PluginCapabilities.ProvisioningPanelPackages"/>. A plugin that does not implement
/// the interface is treated as <see cref="PanelOperationOutcome.NotSupported"/>, and the admin types the package name by hand.
/// </summary>
/// <remarks>
/// The rules of <see cref="IProvisioningPanelPlugin"/> hold here: the server arrives with its stored credentials, the
/// operation is a read and safe to repeat, a refusal is an outcome and never an exception, and the panel's own text is
/// never returned.
/// </remarks>
public interface IProvisioningPanelPackagesPlugin : IProvisioningPanelPlugin
{
    /// <summary>
    /// Lists the packages the server offers. Read only; safe to repeat. Every package has a name that is not empty and
    /// unique in the listing, and no limit is negative. An empty list is an empty list, not an absent operation.
    /// </summary>
    /// <param name="context">The server whose packages are wanted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>Succeeded</c> with the packages, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<ValueResult<IReadOnlyList<PanelPackageDetails>, PanelRefusalReason>> ListPackagesAsync(PanelListPackagesContext context, CancellationToken ct);
}
