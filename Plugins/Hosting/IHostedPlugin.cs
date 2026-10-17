namespace Innovayse.SDK.Plugins.Hosting;

/// <summary>
/// Contract for a hosted plugin (<see cref="Enums.PluginType.Hosted"/>): a background worker the host
/// starts after its own services are up and stops on shutdown. A plugin may implement this
/// beside another contract; the host calls it once per process.
/// </summary>
public interface IHostedPlugin
{
    /// <summary>Starts the worker. Return once it is running; do not block on its loop.</summary>
    /// <param name="ct">Cancelled when the host gives up starting.</param>
    /// <returns>A task that completes when the worker has started.</returns>
    Task StartAsync(CancellationToken ct);

    /// <summary>Stops the worker gracefully.</summary>
    /// <param name="ct">Cancelled when the host's shutdown grace period ends.</param>
    /// <returns>A task that completes when the worker has stopped.</returns>
    Task StopAsync(CancellationToken ct);
}
