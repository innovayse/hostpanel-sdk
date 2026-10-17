using Innovayse.SDK.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Innovayse.SDK.Base;
/// <summary>
/// Base class for all domain registrar plugins.
/// Provides <see cref="GetConfig"/> and a structured <see cref="Logger"/>.
/// </summary>
public abstract class RegistrarProviderBase : IRegistrarPlugin
{
    /// <summary>The plugin id this class uses.</summary>
    private readonly string _pluginId;

    /// <summary>The configuration this class uses.</summary>
    private readonly IConfiguration _configuration;

    /// <summary>Initializes a new instance of the <see cref="RegistrarProviderBase"/> class.</summary>
    /// <param name="pluginId">The plugin id this class uses.</param>
    /// <param name="configuration">The configuration this class uses.</param>
    /// <param name="logger">The logger this class uses.</param>
    protected RegistrarProviderBase(string pluginId, IConfiguration configuration, ILogger logger)
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
