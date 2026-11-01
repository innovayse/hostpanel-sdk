namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>The <c>api</c> block of <c>plugin.json</c>: the routes the host forwards to the plugin.</summary>
public sealed class PluginApi
{
    /// <summary>Backing field of <see cref="Routes"/>; empty unless the manifest lists some.</summary>
    private readonly IReadOnlyList<PluginRoute> _routes = [];

    /// <summary>Gets the declared routes. A missing or <c>null</c> <c>routes</c> reads as an empty list.</summary>
    public IReadOnlyList<PluginRoute> Routes
    {
        get => _routes;
        init => _routes = value ?? [];
    }
}
