namespace Innovayse.SDK.Plugins.Store;

/// <summary>
/// The plugin's own key and value storage, kept by the host. It is scoped to the plugin and to the opaque client
/// reference of the current call; on a call without a client it is scoped to the plugin alone. A plugin never reaches
/// another plugin's or another client's values, and no method takes a scope argument. Values are text; a plugin that
/// stores JSON serialises it itself.
/// </summary>
public interface IPluginStore
{
    /// <summary>Reads a value.</summary>
    /// <param name="key">The key: 1 to 200 characters of letters, digits, <c>.</c>, <c>-</c>, <c>_</c> and <c>:</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The value, or <see langword="null"/> when the key is not set.</returns>
    Task<string?> GetAsync(string key, CancellationToken ct);

    /// <summary>Writes a value, replacing an existing one.</summary>
    /// <param name="key">The key, as for <see cref="GetAsync"/>.</param>
    /// <param name="value">The text to keep; the host caps its size.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the value is saved.</returns>
    Task SetAsync(string key, string value, CancellationToken ct);

    /// <summary>Removes a value.</summary>
    /// <param name="key">The key, as for <see cref="GetAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when a value was removed, <see langword="false"/> when the key was not set.</returns>
    Task<bool> RemoveAsync(string key, CancellationToken ct);
}
