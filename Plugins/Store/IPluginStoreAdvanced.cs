namespace Hostpanel.Sdk.Plugins.Store;

/// <summary>
/// Optional capability of the plugin's key and value storage: values that expire, listing the keys under a prefix, and
/// a conditional write. It is what a plugin that handles webhooks needs to remember that an event was already handled,
/// to expire old markers and to keep two processes from handling the same event. The store the host gives a plugin
/// implements it when the host supports it, and <see cref="IPluginStore"/> does not change, so a plugin checks with a
/// type test (<c>store is IPluginStoreAdvanced</c>) and falls back to what it does today when the host lacks it. The
/// interface is new and extends the old one, so the change is source and binary compatible. The scope is the one of
/// <see cref="IPluginStore"/>. The limits are the host's and are listed in <see cref="PluginStoreLimits"/>.
/// <para>
/// A plain <see cref="IPluginStore.SetAsync"/> replaces the value and clears any time to live: the key then lives
/// until it is removed. To keep an expiry the plugin writes with a time to live again.
/// </para>
/// </summary>
public interface IPluginStoreAdvanced : IPluginStore
{
    /// <summary>
    /// Writes a value that expires, replacing an existing one and its expiry. Once the time to live has passed the key
    /// reads as absent from every method of the store, and the host removes it.
    /// </summary>
    /// <param name="key">The key, as for <see cref="IPluginStore.GetAsync"/>.</param>
    /// <param name="value">The text to keep; the host caps its size.</param>
    /// <param name="timeToLive">
    /// How long the value lives, longer than zero and at most <see cref="PluginStoreLimits.MaxTimeToLive"/>.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the value is saved.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="timeToLive"/> is zero, negative or above the limit; nothing is written.
    /// </exception>
    /// <exception cref="PluginStoreQuotaException">
    /// The key is new and the store already holds <see cref="PluginStoreLimits.MaxKeys"/> keys; nothing is written.
    /// </exception>
    Task SetAsync(string key, string value, TimeSpan timeToLive, CancellationToken ct);

    /// <summary>
    /// Lists the keys that start with a prefix, in ascending ordinal order. An expired key is not listed. The prefix
    /// follows the rules of a key (letters, digits, <c>.</c>, <c>-</c>, <c>_</c> and <c>:</c>, up to 200 characters),
    /// is case sensitive and is matched ordinally, and an empty prefix lists every key. There is no paging: a plugin
    /// keeps its markers under a prefix that stays small, or removes what it has handled.
    /// </summary>
    /// <param name="prefix">The prefix, as a key may start.</param>
    /// <param name="limit">The most keys to return, from 1 to <see cref="PluginStoreLimits.MaxListLimit"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>At most <paramref name="limit"/> keys; empty when none match.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="limit"/> is below 1 or above the limit.
    /// </exception>
    Task<IReadOnlyList<string>> ListKeysAsync(string prefix, int limit, CancellationToken ct);

    /// <summary>
    /// Writes a value only when the key does not exist. The check and the write are one atomic step, so when two
    /// processes call it for one key at the same time exactly one of them gets <see langword="true"/>. An expired key
    /// counts as absent. It never changes an existing value.
    /// </summary>
    /// <param name="key">The key, as for <see cref="IPluginStore.GetAsync"/>.</param>
    /// <param name="value">The text to keep; the host caps its size.</param>
    /// <param name="timeToLive">
    /// How long the value lives, as for the overload of <c>SetAsync</c>; <see langword="null"/> keeps it until it is
    /// removed.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see langword="true"/> when the value was written, <see langword="false"/> when the key already existed.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="timeToLive"/> is zero, negative or above the limit; nothing is written.
    /// </exception>
    /// <exception cref="PluginStoreQuotaException">
    /// The key is absent and the store already holds <see cref="PluginStoreLimits.MaxKeys"/> keys; nothing is written.
    /// A key that exists answers <see langword="false"/> and never throws it.
    /// </exception>
    Task<bool> SetIfAbsentAsync(string key, string value, TimeSpan? timeToLive, CancellationToken ct);
}
