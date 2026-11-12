using Hostpanel.Sdk.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>A payment gateway base class with its settings reader opened.</summary>
public sealed class PaymentSettingsProbe : PaymentGatewayBase, ISettingsProbe
{
    /// <summary>Initializes a new instance of the <see cref="PaymentSettingsProbe"/> class.</summary>
    /// <param name="pluginId">The plugin's id.</param>
    /// <param name="configuration">The settings.</param>
    /// <param name="logger">The logger.</param>
    public PaymentSettingsProbe(string pluginId, IConfiguration configuration, ILogger logger)
        : base(pluginId, configuration, logger)
    {
    }

    /// <inheritdoc/>
    public string? Read(string key)
    {
        return GetConfig(key);
    }
}
