namespace Hostpanel.Sdk.Plugins.Store;

/// <summary>
/// An in memory <see cref="IPluginStoreAdvanced"/> for the tests of a plugin author: it keeps every promise of the
/// interface, so a plugin can be run against it, and <see cref="PluginStoreContract"/> shows it to be well behaved. Its
/// clock can be moved with <see cref="AdvanceAsync"/>, so expiry is tested without waiting. It is thread safe and holds
/// everything in memory; it is not for production. The members a test double may change are virtual or protected, which
/// lets a test break one promise at a time and watch the contract check reject it.
/// </summary>
public class InMemoryPluginStore : IPluginStoreAdvanced
{
    /// <summary>The sentence that names the rules of a key.</summary>
    private const string KeyRule =
        "A key is 1 to 200 characters of ASCII letters, digits, '.', '-', '_' and ':'.";

    /// <summary>The sentence that names the rules of a prefix.</summary>
    private const string PrefixRule =
        "A prefix is up to 200 characters of ASCII letters, digits, '.', '-', '_' and ':'.";

    /// <summary>Guards the entries so the conditional write is atomic.</summary>
    private readonly object _gate = new();

    /// <summary>Holds the value and the expiry of each key; a null expiry never expires.</summary>
    private readonly Dictionary<string, (string Value, DateTimeOffset? ExpiresAt)> _entries = [];

    /// <summary>The most keys the store holds.</summary>
    private readonly int _maxKeys;

    /// <summary>How far the clock of the store has been moved from the real one.</summary>
    private TimeSpan _offset;

    /// <summary>Initializes a new instance of the <see cref="InMemoryPluginStore"/> class.</summary>
    /// <param name="maxKeys">The key quota; <see cref="PluginStoreLimits.MaxKeys"/> by default.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxKeys"/> is below 1.</exception>
    public InMemoryPluginStore(int maxKeys = PluginStoreLimits.MaxKeys)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxKeys, 1);
        _maxKeys = maxKeys;
    }

    /// <summary>Gets the lock that guards the entries; a member that reads or changes them holds it.</summary>
    protected object Gate => _gate;

    /// <summary>Gets the time the store believes it is: the real time, moved by <see cref="AdvanceAsync"/>.</summary>
    protected DateTimeOffset Now => DateTimeOffset.UtcNow + _offset;

    /// <summary>Moves the clock of the store forward, so a value with a time to live can expire without
    /// waiting.</summary>
    /// <param name="elapsed">How much time passes.</param>
    /// <returns>A completed task.</returns>
    public Task AdvanceAsync(TimeSpan elapsed)
    {
        lock (_gate)
        {
            _offset += elapsed;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken ct)
    {
        ValidateKey(key);
        lock (_gate)
        {
            return Task.FromResult(IsLive(key) ? _entries[key].Value : null);
        }
    }

    /// <inheritdoc />
    public virtual Task SetAsync(string key, string value, CancellationToken ct)
    {
        lock (_gate)
        {
            Put(key, value, null);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string value, TimeSpan timeToLive, CancellationToken ct)
    {
        ValidateTimeToLive(timeToLive);
        lock (_gate)
        {
            Put(key, value, timeToLive);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string key, CancellationToken ct)
    {
        ValidateKey(key);
        lock (_gate)
        {
            return Task.FromResult(IsLive(key) && _entries.Remove(key));
        }
    }

    /// <inheritdoc />
    public virtual Task<IReadOnlyList<string>> ListKeysAsync(string prefix, int limit, CancellationToken ct)
    {
        if (!PluginStoreKeys.IsValidPrefix(prefix))
        {
            throw new ArgumentException(PrefixRule, nameof(prefix));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, PluginStoreLimits.MaxListLimit);
        lock (_gate)
        {
            var keys = LiveKeys()
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
                .Take(limit)
                .ToList();
            return Task.FromResult<IReadOnlyList<string>>(keys);
        }
    }

    /// <inheritdoc />
    public virtual Task<bool> SetIfAbsentAsync(
        string key,
        string value,
        TimeSpan? timeToLive,
        CancellationToken ct)
    {
        ValidateKey(key);
        if (timeToLive is { } span)
        {
            ValidateTimeToLive(span);
        }

        lock (_gate)
        {
            if (IsLive(key))
            {
                return Task.FromResult(false);
            }

            Put(key, value, timeToLive);
            return Task.FromResult(true);
        }
    }

    /// <summary>Checks a key against <see cref="PluginStoreKeys"/>, the rules the host keeps.</summary>
    /// <param name="key">The key to check.</param>
    /// <exception cref="ArgumentException">The key is empty, longer than 200 characters or has a character not
    /// allowed.</exception>
    protected static void ValidateKey(string key)
    {
        if (!PluginStoreKeys.IsValidKey(key))
        {
            throw new ArgumentException(KeyRule, nameof(key));
        }
    }

    /// <summary>Checks a time to live against the limits of <see cref="PluginStoreLimits"/>.</summary>
    /// <param name="timeToLive">The time to live to check.</param>
    /// <exception cref="ArgumentOutOfRangeException">The time to live is zero, negative or above the limit.</exception>
    protected virtual void ValidateTimeToLive(TimeSpan timeToLive)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeToLive, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(timeToLive, PluginStoreLimits.MaxTimeToLive);
    }

    /// <summary>Tells whether a value with this expiry has expired.</summary>
    /// <param name="expiresAt">The moment the value expires.</param>
    /// <returns><see langword="true"/> when the store's clock is at or past the moment.</returns>
    protected virtual bool IsExpired(DateTimeOffset expiresAt)
    {
        return expiresAt <= Now;
    }

    /// <summary>Tells whether a key exists and has not expired. The caller holds <see cref="Gate"/>.</summary>
    /// <param name="key">The key.</param>
    /// <returns><see langword="true"/> when the key can be read.</returns>
    protected bool IsLive(string key)
    {
        return _entries.TryGetValue(key, out var entry) && (entry.ExpiresAt is not { } at || !IsExpired(at));
    }

    /// <summary>Lists the keys that exist and have not expired. The caller holds <see cref="Gate"/>.</summary>
    /// <returns>The live keys, in no order.</returns>
    protected IEnumerable<string> LiveKeys()
    {
        return _entries.Keys.Where(IsLive);
    }

    /// <summary>Reads the expiry of a key. The caller holds <see cref="Gate"/>.</summary>
    /// <param name="key">The key.</param>
    /// <returns>The moment the value expires, or <see langword="null"/> when it never does or the key is
    /// absent.</returns>
    protected DateTimeOffset? ExpiryOf(string key)
    {
        return _entries.TryGetValue(key, out var entry) ? entry.ExpiresAt : null;
    }

    /// <summary>
    /// Writes a value and its expiry, replacing what was there. The caller holds <see cref="Gate"/>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="timeToLive">How long it lives, or <see langword="null"/> for ever.</param>
    /// <exception cref="PluginStoreQuotaException">The key is new and the store holds the most keys it may.</exception>
    protected void Put(string key, string value, TimeSpan? timeToLive)
    {
        PutUntil(key, value, timeToLive is { } span ? Now + span : null);
    }

    /// <summary>
    /// Writes a value that expires at a given moment, replacing what was there. The caller holds <see cref="Gate"/>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="expiresAt">The moment it expires, or <see langword="null"/> for ever.</param>
    /// <exception cref="PluginStoreQuotaException">The key is new and the store holds the most keys it may.</exception>
    protected void PutUntil(string key, string value, DateTimeOffset? expiresAt)
    {
        ValidateKey(key);
        if (!IsLive(key) && LiveKeys().Count() >= _maxKeys)
        {
            throw new PluginStoreQuotaException();
        }

        _entries[key] = (value, expiresAt);
    }
}
