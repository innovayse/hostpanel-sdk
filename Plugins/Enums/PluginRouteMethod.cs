namespace Innovayse.SDK.Plugins.Enums;

/// <summary>
/// The HTTP method of one route a plugin declares in <c>api</c>. Values start at 1; 0 is not a method.
/// </summary>
public enum PluginRouteMethod
{
    /// <summary>Reads a resource.</summary>
    Get = 1,

    /// <summary>Creates a resource or runs an action.</summary>
    Post = 2,

    /// <summary>Replaces a resource.</summary>
    Put = 3,

    /// <summary>Changes part of a resource.</summary>
    Patch = 4,

    /// <summary>Removes a resource.</summary>
    Delete = 5,
}
