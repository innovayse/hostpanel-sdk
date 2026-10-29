using Innovayse.SDK.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Innovayse.SDK.Base;

/// <summary>
/// Base class for all provisioning provider plugins.
/// Provides <see cref="GetConfig"/> and a structured <see cref="Logger"/>.
/// </summary>
public abstract class ProvisioningProviderBase : IProvisioningPlugin
{
    /// <summary>The plugin's id, the middle segment of every configuration key it reads.</summary>
    private readonly string _pluginId;

    /// <summary>Where <see cref="GetConfig"/> reads the plugin's saved settings.</summary>
    private readonly IConfiguration _configuration;

    /// <summary>Initializes a new instance of the <see cref="ProvisioningProviderBase"/> class.</summary>
    /// <param name="pluginId">The plugin's id, the middle segment of every configuration key it reads.</param>
    /// <param name="configuration">Where <see cref="GetConfig"/> reads the plugin's saved settings.</param>
    /// <param name="logger">The structured logger exposed to the plugin as <see cref="Logger"/>.</param>
    protected ProvisioningProviderBase(string pluginId, IConfiguration configuration, ILogger logger)
    {
        _pluginId = pluginId;
        _configuration = configuration;
        Logger = logger;
    }

    /// <summary>Gets the structured logger pre-injected for this provider.</summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Reads a configuration value from the Settings table via the standard key
    /// <c>integration:{pluginId}:{key}</c>.
    /// </summary>
    /// <param name="key">The field key as declared in <c>plugin.json</c>.</param>
    /// <returns>The stored value, or <see langword="null"/> if not set.</returns>
    protected string? GetConfig(string key)
        => _configuration[$"integration:{_pluginId}:{key}"];
}
