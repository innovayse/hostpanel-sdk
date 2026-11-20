using Hostpanel.Sdk.Plugins.Provisioning;

namespace Hostpanel.Sdk.Plugins.Host;

/// <summary>
/// Optional capability of the host API: resolves one of the call's client's own services to the server it runs on and
/// to the account the panel holds for it, so a plugin route on a service tab can talk to that panel. It extends
/// <see cref="IPluginHostApi"/> and that interface does not change, so a plugin checks with a type test
/// (<c>host is IPluginHostServiceConnectionApi</c>) and does without when the host lacks it. The interface is new, so the
/// change is source and binary compatible. Like every member of the host API it is bound to the call's client: it takes a
/// service id, never a client id, and another client's service answers like a missing one.
/// </summary>
/// <remarks>
/// Allowed only on a client-area call, and only for a service whose product is bound to the plugin; otherwise a
/// <see cref="PluginHostAccessException"/> is thrown. The plugin must also declare <see cref="PluginHostAccess.ServiceConnection"/>
/// (<c>serviceConnection</c>) in its manifest <c>hostAccess</c>, or the host does not answer. The host shows the declared
/// kinds to the operator at install, the same rule as <see cref="PluginHostAccess.Ssh"/>,
/// <see cref="PluginHostAccess.Websites"/> and <see cref="PluginHostAccess.Databases"/>. The answer carries the stored server credentials
/// (<see cref="PanelServerConnection"/>), so a plugin sends them only to its own panel and never logs or returns them.
/// </remarks>
public interface IPluginHostServiceConnectionApi : IPluginHostApi
{
    /// <summary>Resolves one service of the caller to its server connection and its provisioning reference.</summary>
    /// <param name="serviceId">The caller's service, one of the ids <see cref="IPluginHostApi.ListServicesAsync"/> answers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The server the service runs on and the service with <see cref="PanelService.ProvisioningRef"/> set, the same shape
    /// a provisioning operation receives.
    /// </returns>
    /// <exception cref="PluginHostAccessException">
    /// The host refused: <see cref="Enums.PluginHostAccessFailure.NotAllowed"/> off the client area or for a product that
    /// is not bound to the plugin, <see cref="Enums.PluginHostAccessFailure.NotFound"/> for a service that is not the
    /// caller's, <see cref="Enums.PluginHostAccessFailure.Unavailable"/> for a service that has no account on a server yet.
    /// </exception>
    Task<PanelAccountContext> ResolveServiceConnectionAsync(int serviceId, CancellationToken ct);
}
