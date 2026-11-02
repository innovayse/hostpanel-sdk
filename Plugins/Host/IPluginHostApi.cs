namespace Innovayse.SDK.Plugins.Host;

/// <summary>
/// Read-only facts the host shares with a plugin about the client of the current call. The instance is bound to that
/// client by the host: no method takes a client id, so a plugin cannot ask for another client's data. On a call without
/// a client the profile is <see langword="null"/> and the lists are empty.
/// </summary>
public interface IPluginHostApi
{
    /// <summary>Reads the profile of the call's client.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The profile, or <see langword="null"/> when the call has no client.</returns>
    Task<PluginClientProfile?> GetProfileAsync(CancellationToken ct);

    /// <summary>Lists the services of the call's client, newest first. The host returns at most the 50 most recent; older ones are not reachable.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The services; empty when there are none or the call has no client.</returns>
    Task<IReadOnlyList<PluginClientService>> ListServicesAsync(CancellationToken ct);

    /// <summary>Lists the invoices of the call's client, newest first. The host returns at most the 50 most recent; older ones are not reachable.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The invoices; empty when there are none or the call has no client.</returns>
    Task<IReadOnlyList<PluginClientInvoice>> ListInvoicesAsync(CancellationToken ct);

    /// <summary>
    /// Issues an SSH identity for one hosting service of the caller: the host generates an Ed25519 key, installs its
    /// public half on the service's hosting account like a key the client adds, and returns the private half once.
    /// The key is listed among the service's SSH keys, so the client sees and can revoke it.
    /// </summary>
    /// <remarks>
    /// Allowed only on a client-area call, when the manifest declares <see cref="PluginHostAccess.Ssh"/> and the
    /// service's product is bound to the plugin; otherwise a <see cref="PluginHostAccessException"/> is thrown.
    /// </remarks>
    /// <param name="serviceId">The caller's service.</param>
    /// <param name="label">What the key is for, shown to the client next to it.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Where to connect and the private key.</returns>
    /// <exception cref="PluginHostAccessException">The host refused; <see cref="PluginHostAccessException.Failure"/> says why.</exception>
    Task<PluginSshAccess> IssueSshAccessAsync(int serviceId, string label, CancellationToken ct);

    /// <summary>Revokes an SSH identity this plugin was issued for one of the caller's services.</summary>
    /// <remarks>Allowed as for <see cref="IssueSshAccessAsync"/>, except that the product need no longer be bound.</remarks>
    /// <param name="serviceId">The caller's service.</param>
    /// <param name="keyId">The key, as <see cref="PluginSshAccess.KeyId"/> named it.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes once the key is off the account.</returns>
    /// <exception cref="PluginHostAccessException">
    /// The host refused, or the key is not one this plugin was issued for the service.
    /// </exception>
    Task RevokeSshAccessAsync(int serviceId, Guid keyId, CancellationToken ct);
}
