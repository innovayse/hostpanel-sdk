using Hostpanel.Sdk.Plugins.Store;

namespace Hostpanel.Sdk.Tests.Fakes;

/// <summary>
/// An <see cref="InMemoryPluginStore"/> that keeps every promise by default; each switch breaks one of them, so the
/// contract helper can be shown to reject it.
/// </summary>
internal sealed class BreakableStore : InMemoryPluginStore
{
    /// <summary>Gets a value indicating whether the conditional write checks and writes in two steps.</summary>
    public bool NonAtomic { get; init; }

    /// <summary>Gets a value indicating whether an expired key is still readable and listed.</summary>
    public bool IgnoreExpiry { get; init; }

    /// <summary>Gets a value indicating whether the listing ignores the prefix.</summary>
    public bool ListEverything { get; init; }

    /// <summary>Gets a value indicating whether the listing ignores the limit.</summary>
    public bool IgnoreLimit { get; init; }

    /// <summary>Gets a value indicating whether a plain write keeps the expiry of the value it replaces.</summary>
    public bool KeepTimeToLive { get; init; }

    /// <summary>Gets a value indicating whether any time to live is accepted.</summary>
    public bool AcceptAnyTimeToLive { get; init; }

    /// <inheritdoc />
    public override Task SetAsync(string key, string value, CancellationToken ct)
    {
        if (!KeepTimeToLive)
        {
            return base.SetAsync(key, value, ct);
        }

        lock (Gate)
        {
            PutUntil(key, value, ExpiryOf(key));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task<IReadOnlyList<string>> ListKeysAsync(string prefix, int limit, CancellationToken ct)
    {
        if (!ListEverything && !IgnoreLimit)
        {
            return base.ListKeysAsync(prefix, limit, ct);
        }

        lock (Gate)
        {
            var keys = LiveKeys()
                .Where(key => ListEverything || key.StartsWith(prefix, StringComparison.Ordinal))
                .Order(StringComparer.Ordinal);
            return Task.FromResult<IReadOnlyList<string>>((IgnoreLimit ? keys : keys.Take(limit)).ToList());
        }
    }

    /// <inheritdoc />
    public override async Task<bool> SetIfAbsentAsync(
        string key,
        string value,
        TimeSpan? timeToLive,
        CancellationToken ct)
    {
        if (!NonAtomic)
        {
            return await base.SetIfAbsentAsync(key, value, timeToLive, ct);
        }

        var exists = await GetAsync(key, ct) is not null;

        // The pause lets every other caller finish its check before anyone writes, which is what makes it not atomic.
        await Task.Delay(50, ct);
        if (exists)
        {
            return false;
        }

        lock (Gate)
        {
            Put(key, value, timeToLive);
        }

        return true;
    }

    /// <inheritdoc />
    protected override bool IsExpired(DateTimeOffset expiresAt)
    {
        return !IgnoreExpiry && base.IsExpired(expiresAt);
    }

    /// <inheritdoc />
    protected override void ValidateTimeToLive(TimeSpan timeToLive)
    {
        if (!AcceptAnyTimeToLive)
        {
            base.ValidateTimeToLive(timeToLive);
        }
    }
}
