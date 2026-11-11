using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Plugins.VirtualServers;

/// <summary>
/// The listing of the servers a platform holds, as a separate opt in for reconciliation: a virtual server plugin implements
/// it next to <see cref="IVirtualServerPlugin"/> and lists <see cref="PluginCapabilities.VirtualServerList"/> in its
/// manifest. A host treats a plugin that does not implement it as <see cref="PanelOperationOutcome.NotSupported"/>. It is a
/// separate interface because a new operation always arrives as a new interface, never as a member of an existing one.
/// </summary>
public interface IVirtualServerListPlugin : IVirtualServerPlugin
{
    /// <summary>
    /// Lists the servers the platform holds, one page per call, so the host can reconcile its services against the platform.
    /// Read only. Following <see cref="PanelAccountListResult.NextCursor"/> to the end returns every server exactly once. A
    /// server is a <see cref="PanelAccount"/>: its <see cref="PanelAccount.ProvisioningRef"/> is the reference
    /// <see cref="IVirtualServerPlugin.CreateServerAsync"/> returned, <see cref="PanelAccount.Domain"/> holds the server name,
    /// <see cref="PanelAccount.Status"/> is active or suspended, and <see cref="PanelAccount.PackageName"/> is the plan.
    /// </summary>
    /// <param name="context">The platform and the cursor of the page wanted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A page, <c>NotSupported</c> or <c>Refused</c>.</returns>
    Task<PanelAccountListResult> ListServersAsync(PanelListAccountsContext context, CancellationToken ct);
}
