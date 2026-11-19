namespace Hostpanel.Sdk.Plugins.Host;

/// <summary>
/// Optional capability of the host API: websites and databases on a client's hosting service. It extends
/// <see cref="IPluginHostApi"/> and that interface does not change, so a plugin checks with a type test
/// (<c>host is IPluginHostHostingApi</c>) and does without when the host lacks it. The interface is new, so the change
/// is source and binary compatible. Every method is bound to the call's client, like <see cref="IPluginHostApi"/>.
/// </summary>
/// <remarks>
/// Allowed only on a client-area call, when the manifest declares the matching <see cref="PluginHostAccess"/> kind and
/// the service's product is bound to the plugin; otherwise a <see cref="PluginHostAccessException"/> is thrown.
/// </remarks>
public interface IPluginHostHostingApi : IPluginHostApi
{
    /// <summary>Reads what the hosting account of one service can do.</summary>
    /// <param name="serviceId">The caller's service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The capabilities of the service.</returns>
    /// <exception cref="PluginHostAccessException">The host refused; <see cref="PluginHostAccessException.Failure"/> says why.</exception>
    Task<PluginHostingCapabilities> GetHostingCapabilitiesAsync(int serviceId, CancellationToken ct);

    /// <summary>
    /// Makes sure a website exists on a hosting service as the specification says. A repeat for the same host name
    /// answers the existing website and updates it to the specification, so the call is safe to retry.
    /// </summary>
    /// <remarks>Requires <see cref="PluginHostAccess.Websites"/>.</remarks>
    /// <param name="serviceId">The caller's service.</param>
    /// <param name="spec">The website to set up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The website as the host holds it.</returns>
    /// <exception cref="PluginHostAccessException">
    /// The host refused: <see cref="Enums.PluginHostAccessFailure.InvalidRequest"/> for a specification it cannot
    /// accept, <see cref="Enums.PluginHostAccessFailure.Conflict"/> when the host name belongs to another website.
    /// </exception>
    Task<PluginWebsite> EnsureWebsiteAsync(int serviceId, PluginWebsiteSpec spec, CancellationToken ct);

    /// <summary>Removes a website this plugin set up on one of the caller's services.</summary>
    /// <remarks>Allowed as for <see cref="EnsureWebsiteAsync"/>, except that the product need no longer be bound.</remarks>
    /// <param name="serviceId">The caller's service.</param>
    /// <param name="websiteId">The website, as <see cref="PluginWebsite.Id"/> named it.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes once the website is gone from the account.</returns>
    /// <exception cref="PluginHostAccessException">The host refused, or the website is not one this plugin set up for the service.</exception>
    Task RemoveWebsiteAsync(int serviceId, Guid websiteId, CancellationToken ct);

    /// <summary>
    /// Makes sure a database exists on a hosting service for the label. A repeat for the same label answers the
    /// existing database with a null password, because the password is returned only when the database is created.
    /// </summary>
    /// <remarks>Requires <see cref="PluginHostAccess.Databases"/>.</remarks>
    /// <param name="serviceId">The caller's service.</param>
    /// <param name="label">What the database is for, shown to the client next to it.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The database as the host holds it, with the password only on creation.</returns>
    /// <exception cref="PluginHostAccessException">
    /// The host refused: <see cref="Enums.PluginHostAccessFailure.LimitReached"/> when the service holds as many
    /// databases as the host allows.
    /// </exception>
    Task<PluginDatabase> EnsureDatabaseAsync(int serviceId, string label, CancellationToken ct);

    /// <summary>Removes a database this plugin created on one of the caller's services.</summary>
    /// <remarks>Allowed as for <see cref="EnsureDatabaseAsync"/>, except that the product need no longer be bound.</remarks>
    /// <param name="serviceId">The caller's service.</param>
    /// <param name="databaseId">The database, as <see cref="PluginDatabase.Id"/> named it.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes once the database is gone from the account.</returns>
    /// <exception cref="PluginHostAccessException">The host refused, or the database is not one this plugin created for the service.</exception>
    Task RemoveDatabaseAsync(int serviceId, Guid databaseId, CancellationToken ct);
}
