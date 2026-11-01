using Innovayse.SDK.Plugins.Enums;

namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>One HTTP route a plugin answers, an entry of <c>api.client</c> or <c>api.admin</c> in <c>plugin.json</c>.</summary>
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
    /// Gets the permission the caller needs: a client permission name, or <see cref="PluginPermissions.Any"/> for every signed-in caller.
    /// <see langword="null"/> when the route declares none, and always so for a route of <c>api.admin</c>, which
    /// takes no permission.
    /// </summary>
    public string? Permission { get; init; }

    /// <summary>
    /// Gets the rate limit policy the route asks for. <see langword="null"/> means
    /// <see cref="PluginRateLimitPolicy.Default"/>.
    /// </summary>
    public PluginRateLimitPolicy? RateLimit { get; init; }

    /// <summary>
    /// Gets a value indicating whether the route answers with a stream of events instead of one reply. A
    /// streaming route is held to a concurrency cap as well as to its rate limit.
    /// </summary>
    public bool Streaming { get; init; }
}
