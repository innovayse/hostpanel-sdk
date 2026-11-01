namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>
/// The <c>api</c> block of <c>plugin.json</c>: the routes the host forwards to the plugin, one list per panel
/// (mirroring <see cref="PluginUi"/>). A client route may carry a <see cref="PluginRoute.Permission"/>; an admin route
/// never does, because the admin area is guarded by the admin policy alone.
/// </summary>
public sealed class PluginApi
{
    /// <summary>Backing field of <see cref="Client"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginRoute> _client = [];

    /// <summary>Backing field of <see cref="Admin"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginRoute> _admin = [];

    /// <summary>
    /// Gets the routes the client panel may call, reached under the client plugin prefix. A missing or <c>null</c>
    /// <c>client</c> reads as an empty list.
    /// </summary>
    public IReadOnlyList<PluginRoute> Client
    {
        get => _client;
        init => _client = value ?? [];
    }

    /// <summary>
    /// Gets the routes the admin panel may call, reached under the admin plugin prefix. Their
    /// <see cref="PluginRoute.Permission"/> is <see langword="null"/>: an admin route declares no permission. A
    /// missing or <c>null</c> <c>admin</c> reads as an empty list.
    /// </summary>
    public IReadOnlyList<PluginRoute> Admin
    {
        get => _admin;
        init => _admin = value ?? [];
    }
}
