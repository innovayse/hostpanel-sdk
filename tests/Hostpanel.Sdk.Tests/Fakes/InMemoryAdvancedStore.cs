using Hostpanel.Sdk.Plugins.Store;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// An in memory <see cref="IPluginStoreAdvanced"/> with a clock the test moves. By default it keeps every promise of
/// the interface; each switch breaks one of them, so the contract helper can be shown to reject it.
/// </summary>
internal sealed class InMemoryAdvancedStore : IPluginStoreAdvanced
{
    /// <summary>Guards <see cref="_entries"/> so the well behaved conditional write is atomic.</summary>
    private readonly object _gate = new();

    /// <summary>Holds the value and the expiry of each key; a null expiry never expires.</summary>
    private readonly Dictionary<string, (string Value, DateTimeOffset? ExpiresAt)> _entries = [];

    /// <summary>Gets or sets the time the store believes it is, which <see cref="AdvanceAsync"/> moves.</summary>
    public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Gets a value indicating whether the conditional write checks and writes in two steps.</summary>
    public bool NonAtomic { get; init; }

    /// <summary>Gets a value indicating whether an expired key is still readable and listed.</summary>
    public bool IgnoreExpiry { get; init; }

    /// <summary>Gets a value indicating whether the listing ignores the prefix.</summary>
    public bool ListEverything { get; init; }

    /// <summary>Gets a value indicating whether the listing ignores the limit.</summary>
    public bool IgnoreLimit { get; init; }

    /// <summary>Moves the clock of the store.</summary>
    /// <param name="elapsed">How much time passes.</param>
    /// <returns>A completed task.</returns>
    public Task AdvanceAsync(TimeSpan elapsed)
    {
        Now += elapsed;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken ct)
    {
        lock (_gate)
        {
            return Task.FromResult(Live(key) ? _entries[key].Value : null);
        }
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string value, CancellationToken ct)
    {
        lock (_gate)
        {
            _entries[key] = (value, null);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string value, TimeSpan timeToLive, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeToLive, TimeSpan.Zero);
        lock (_gate)
        {
            _entries[key] = (value, Now + timeToLive);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken ct)
    {
        lock (_gate)
        {
            return Task.FromResult(_entries.Remove(key));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, int limit, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        lock (_gate)
        {
            var keys = _entries.Keys
                .Where(key => Live(key) && (ListEverything || key.StartsWith(prefix, StringComparison.Ordinal)))
                .Order(StringComparer.Ordinal);
            return Task.FromResult<IReadOnlyList<string>>((IgnoreLimit ? keys : keys.Take(limit)).ToList());
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetIfAbsentAsync(string key, string value, TimeSpan? timeToLive, CancellationToken ct)
    {
        if (NonAtomic)
        {
            var exists = await GetAsync(key, ct) is not null;
            await Task.Delay(50, ct);
            if (exists)
            {
                return false;
            }

            Put(key, value, timeToLive);
            return true;
        }

        lock (_gate)
        {
            if (Live(key))
            {
                return false;
            }

            Put(key, value, timeToLive);
            return true;
        }
    }

    /// <summary>Writes a value and its expiry, replacing what was there.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="timeToLive">How long it lives, or <see langword="null"/> for ever.</param>
    private void Put(string key, string value, TimeSpan? timeToLive)
    {
        lock (_gate)
        {
            _entries[key] = (value, timeToLive is { } span ? Now + span : null);
        }
    }

    /// <summary>Tells whether a key exists and has not expired.</summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> when the key can be read.</returns>
    private bool Live(string key)
    {
        return _entries.TryGetValue(key, out var entry)
            && (IgnoreExpiry || entry.ExpiresAt is null || entry.ExpiresAt > Now);
    }
}
