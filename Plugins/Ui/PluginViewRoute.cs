using Innovayse.SDK.Plugins.Enums;
using Innovayse.SDK.Plugins.Manifest;

namespace Innovayse.SDK.Plugins.Ui;

/// <summary>
/// A reference from a view document to one route of the plugin, the value of a member named in
/// <see cref="PluginViews.RouteProperties"/>. A route is identified by its area, method and declared path template;
/// the area is the one of the page or slot that shows the view, so the reference carries the other two.
/// </summary>
public sealed class PluginViewRoute
{
    /// <summary>Gets the HTTP method, the <see cref="PluginRoute.Method"/> of the route named.</summary>
    public required PluginRouteMethod Method { get; init; }

    /// <summary>
    /// Gets the path template exactly as the route declares it in <see cref="PluginRoute.Path"/>, parameter names
    /// included, for example <c>/services/{serviceId}/site</c>.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>Tells whether this reference names a declared route.</summary>
    /// <param name="route">A route of the area that shows the view.</param>
    /// <returns><see langword="true"/> when the method is the same and the path template is the same text, compared exactly.</returns>
    public bool Names(PluginRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);
        return route.Method == Method && string.Equals(route.Path, Path, StringComparison.Ordinal);
    }
}
