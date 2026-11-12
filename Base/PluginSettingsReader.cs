using Microsoft.Extensions.Configuration;

namespace Hostpanel.Sdk.Base;

/// <summary>
/// Reads the settings an administrator saved for one plugin. The three base classes (payment, provisioning and registrar)
/// hold one of these, so the key a setting is stored under and the checks on its arguments are written once.
/// </summary>
internal sealed class PluginSettingsReader
{
    /// <summary>The plugin's id, the middle segment of every configuration key it reads.</summary>
    private readonly string _pluginId;

    /// <summary>Where the plugin's saved settings are read from.</summary>
    private readonly IConfiguration _configuration;

    /// <summary>Initializes a new instance of the <see cref="PluginSettingsReader"/> class.</summary>
    /// <param name="pluginId">The plugin's id, the middle segment of every configuration key it reads.</param>
    /// <param name="configuration">Where the plugin's saved settings are read from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pluginId"/> or <paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="pluginId"/> is empty or white space.</exception>
    internal PluginSettingsReader(string pluginId, IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentNullException.ThrowIfNull(configuration);

        _pluginId = pluginId;
        _configuration = configuration;
    }

    /// <summary>Reads a setting from the Settings table through the standard key <c>integration:{pluginId}:{key}</c>.</summary>
    /// <param name="key">The field key as declared in <c>plugin.json</c>.</param>
    /// <returns>The stored value, or <see langword="null"/> if not set.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty or white space.</exception>
    internal string? Read(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return _configuration[$"integration:{_pluginId}:{key}"];
    }
}
