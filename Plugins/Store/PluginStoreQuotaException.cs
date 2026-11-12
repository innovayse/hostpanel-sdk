namespace Hostpanel.Sdk.Plugins.Store;

/// <summary>
/// Thrown by <see cref="IPluginStoreAdvanced.SetAsync(string, string, TimeSpan, CancellationToken)"/> and
/// <see cref="IPluginStoreAdvanced.SetIfAbsentAsync"/> when writing a new key would pass the key quota
/// (<see cref="PluginStoreLimits.MaxKeys"/>). Replacing the value of a key that exists never throws it. The message
/// holds no key and no value. A plugin that keeps markers removes what it has handled or lets it expire, so the store
/// does not fill.
/// </summary>
public sealed class PluginStoreQuotaException : InvalidOperationException
{
    /// <summary>Initializes a new instance of the <see cref="PluginStoreQuotaException"/> class.</summary>
    public PluginStoreQuotaException()
        : base("The plugin store holds the most keys it may; remove a key or let one expire before writing a new one.")
    {
    }
}
