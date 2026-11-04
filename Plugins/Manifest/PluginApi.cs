namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>
/// The <c>api</c> block of <c>plugin.json</c>: the routes the host forwards to the plugin, one list per panel
/// (mirroring <see cref="PluginUi"/>). A client route may carry a <see cref="PluginRoute.Permission"/>; an admin route
/// never does, because the admin area is guarded by the admin policy alone. <see cref="Callbacks"/> are the routes an
/// outside system calls without a panel session.
/// </summary>
public sealed class PluginApi
{
    /// <summary>Backing field of <see cref="Client"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginRoute> _client = [];

    /// <summary>Backing field of <see cref="Admin"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginRoute> _admin = [];

    /// <summary>Backing field of <see cref="Callbacks"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginRoute> _callbacks = [];

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

    /// <summary>
    /// Gets the callback routes, <c>api.callbacks</c>: requests from outside the panels, for example a gateway telling
    /// the plugin a payment changed. They carry no panel session, so a callback route declares no
    /// <see cref="PluginRoute.Permission"/> and is not <see cref="PluginRoute.Streaming"/>; <see cref="CallbackProblems"/>
    /// lists the entries that break this, and the host refuses a plugin that has any. A callback may set a
    /// <see cref="PluginRoute.RateLimit"/>. The plugin must verify the sender itself. A missing or <c>null</c>
    /// <c>callbacks</c> reads as an empty list.
    /// </summary>
    public IReadOnlyList<PluginRoute> Callbacks
    {
        get => _callbacks;
        init => _callbacks = value ?? [];
    }

    /// <summary>Lists what is wrong with the entries of <see cref="Callbacks"/>.</summary>
    /// <returns>One sentence per offending route, naming it; empty when every callback route is valid.</returns>
    public IReadOnlyList<string> CallbackProblems()
    {
        var problems = new List<string>();
        foreach (var route in _callbacks)
        {
            if (route.Permission is not null)
            {
                problems.Add($"Callback {route.Method} {route.Path} declares a permission; a callback has no signed-in caller.");
            }

            if (route.Streaming)
            {
                problems.Add($"Callback {route.Method} {route.Path} is streaming; a callback answers with one reply.");
            }
        }

        return problems;
    }
}
