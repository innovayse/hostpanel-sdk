using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>One HTTP route a plugin answers, an entry of <c>api.routes</c> in <c>plugin.json</c>.</summary>
public sealed class PluginRoute
{
    /// <summary>Gets the HTTP method.</summary>
    public required PluginRouteMethod Method { get; init; }

    /// <summary>
    /// Gets the path template below the plugin's own prefix: lowercase segments and <c>{parameter}</c> segments,
    /// for example <c>/invoices/{id}</c>.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the permission the caller needs: a client permission name, or <c>Any</c> for every signed-in caller.
    /// <see langword="null"/> when the route declares none.
    /// </summary>
    public string? Permission { get; init; }
}
