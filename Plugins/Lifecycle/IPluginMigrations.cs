using Innovayse.SDK.Plugins.Store;

namespace Innovayse.SDK.Plugins.Lifecycle;

/// <summary>
/// Optional capability of an app plugin that keeps data outside the host (its own database or files): the host calls
/// it when the plugin is enabled and after an update, before the first call is routed to the plugin. A plugin whose
/// state lives in <see cref="IPluginStore"/> does not need it.
/// </summary>
public interface IPluginMigrations
{
    /// <summary>
    /// Brings the plugin's own data up to date. It must be idempotent: the host may call it again after a restart. A
    /// plugin that throws is not routed any calls until the next attempt succeeds.
    /// </summary>
    /// <param name="ct">Cancelled when the host gives up waiting.</param>
    /// <returns>A task that completes when the data is up to date.</returns>
    Task MigrateAsync(CancellationToken ct);
}
