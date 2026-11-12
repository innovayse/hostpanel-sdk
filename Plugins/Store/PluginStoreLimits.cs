namespace Hostpanel.Sdk.Plugins.Store;

/// <summary>
/// The limits of <see cref="IPluginStoreAdvanced"/>. They are the host's: a host never allows more, and may allow less
/// in a deployment, so a plugin that needs a value near a limit reads it from here and handles the refusal.
/// </summary>
public static class PluginStoreLimits
{
    /// <summary>The most keys one <see cref="IPluginStoreAdvanced.ListKeysAsync"/> call returns.</summary>
    public const int MaxListLimit = 200;

    /// <summary>The longest time to live a value may be written with.</summary>
    public static readonly TimeSpan MaxTimeToLive = TimeSpan.FromDays(30);
}
