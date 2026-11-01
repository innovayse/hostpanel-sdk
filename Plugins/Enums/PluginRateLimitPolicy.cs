namespace Innovayse.SDK.Plugins.Enums;

/// <summary>
/// The rate limit policy a plugin route asks for, the <c>rateLimit</c> member of an <c>api</c> route. Values start at 1;
/// 0 is not a policy. A route that names none (<see langword="null"/>) uses <see cref="Default"/>.
/// </summary>
public enum PluginRateLimitPolicy
{
    /// <summary>The ordinary limit applied to a plugin route that names no policy.</summary>
    Default = 1,

    /// <summary>A tighter limit, for a route that is costly or sensitive, such as one that sends messages.</summary>
    Strict = 2,

    /// <summary>A limit for a route that does heavy work, such as a report or an export, and is called rarely.</summary>
    Heavy = 3,
}
