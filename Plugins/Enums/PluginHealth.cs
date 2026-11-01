namespace Innovayse.SDK.Plugins.Enums;

/// <summary>
/// How well a plugin says it is working, the answer of <see cref="Lifecycle.IPluginStatus"/>. Values start at 1;
/// 0 is not a health.
/// </summary>
public enum PluginHealth
{
    /// <summary>The plugin works as expected.</summary>
    Healthy = 1,

    /// <summary>The plugin works with reduced function, for example a dependency answers slowly.</summary>
    Degraded = 2,

    /// <summary>The plugin cannot do its work at the moment.</summary>
    Unavailable = 3,
}
