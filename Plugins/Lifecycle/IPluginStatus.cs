namespace Innovayse.SDK.Plugins.Lifecycle;

/// <summary>
/// Optional capability of an app plugin: it reports how well it works, so the host can show it on the plugin's admin
/// page. A plugin that does not implement it is shown without a status.
/// </summary>
public interface IPluginStatus
{
    /// <summary>Checks the plugin's own dependencies and reports the result.</summary>
    /// <param name="ct">Cancelled when the host stops waiting for the answer.</param>
    /// <returns>The report.</returns>
    Task<PluginStatusReport> GetStatusAsync(CancellationToken ct);
}
