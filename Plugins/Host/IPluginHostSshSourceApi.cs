using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Plugins.Host;

/// <summary>
/// Optional capability of the host API: issues an SSH identity that may only be used from named sources. It extends
/// <see cref="IPluginHostApi"/> and that interface does not change, so a plugin checks with a type test
/// (<c>host is IPluginHostSshSourceApi</c>) and does without when the host lacks it. The interface is new, so the change
/// is source and binary compatible. Since 3.16.0.
/// </summary>
/// <remarks>
/// The same rules as <see cref="IPluginHostApi.IssueSshAccessAsync"/> apply: a client-area call, the manifest declares
/// <see cref="PluginHostAccess.Ssh"/> and the service's product is bound to the plugin.
/// </remarks>
public interface IPluginHostSshSourceApi : IPluginHostApi
{
    /// <summary>
    /// Issues an SSH identity as <see cref="IPluginHostApi.IssueSshAccessAsync"/> does, and limits where the key may be
    /// used from. A connection from any other address is refused by the hosting account.
    /// </summary>
    /// <param name="serviceId">The caller's service.</param>
    /// <param name="label">What the key is for, shown to the client next to it.</param>
    /// <param name="allowedFrom">
    /// The addresses or CIDR ranges, for example <c>203.0.113.7</c> or <c>203.0.113.0/24</c>, the key may be used from.
    /// An empty list puts no limit on the key, which is what <see cref="IPluginHostApi.IssueSshAccessAsync"/> does.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Where to connect and the private key.</returns>
    /// <exception cref="PluginHostAccessException">
    /// The host refused; <see cref="PluginHostAccessException.Failure"/> says why, and
    /// <see cref="PluginHostAccessFailure.InvalidRequest"/> means an entry of <paramref name="allowedFrom"/> is not an
    /// address or a CIDR range.
    /// </exception>
    Task<PluginSshAccess> IssueSshAccessAsync(
        int serviceId,
        string label,
        IReadOnlyList<string> allowedFrom,
        CancellationToken ct);
}
