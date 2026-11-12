using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Plugins.Store;

/// <summary>
/// Checks a plugin author runs in the plugin's own tests, against an in memory store, to prove the promises of
/// <see cref="IPluginStoreAdvanced"/> that the compiler cannot see: an atomic conditional write, expiry and listing.
/// The same checks describe what a host store must do. Each check throws <see cref="PluginContractViolationException"/>
/// naming the promise that was broken. Nothing here depends on a test framework. The checks use keys under a prefix of
/// their own and remove what they wrote.
/// </summary>
public static class PluginStoreContract
{
    /// <summary>The prefix of every key the checks write, so they do not touch the keys of a real test.</summary>
    private const string Prefix = "contract:";

    /// <summary>The time to live the checks give a value: short enough to pass in a test with a moved clock.</summary>
    private static readonly TimeSpan ShortTimeToLive = TimeSpan.FromMinutes(1);

    /// <summary>A moment inside <see cref="ShortTimeToLive"/>, at which the value must still be there.</summary>
    private static readonly TimeSpan HalfOfShortTimeToLive = ShortTimeToLive / 2;

    /// <summary>A moment past <see cref="ShortTimeToLive"/>, at which the value must be gone.</summary>
    private static readonly TimeSpan PastShortTimeToLive = ShortTimeToLive * 2;

    /// <summary>The step by which a time to live is put just outside the limits.</summary>
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Calls <see cref="IPluginStoreAdvanced.SetIfAbsentAsync"/> for one key from several callers at the same time and
    /// asserts that exactly one wins, that the stored value is the winner's, and that a later call changes nothing.
    /// </summary>
    /// <param name="store">The store under test.</param>
    /// <param name="callers">How many callers race for the key; at least 2.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="callers"/> is less than 2.</exception>
    /// <exception cref="PluginContractViolationException">
    /// Not exactly one caller won, the stored value is not the winner's, or a later call overwrote it.
    /// </exception>
    public static async Task AssertSetIfAbsentIsAtomicAsync(
        IPluginStoreAdvanced store,
        int callers = 8,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentOutOfRangeException.ThrowIfLessThan(callers, 2);

        var key = Prefix + "atomic";
        try
        {
            var races = Enumerable.Range(0, callers)
                .Select(caller => Task.Run(() => store.SetIfAbsentAsync(key, "caller-" + caller, null, ct), ct))
                .ToArray();
            var results = await Task.WhenAll(races);

            var winners = results.Count(won => won);
            if (winners != 1)
            {
                throw new PluginContractViolationException(
                    $"SetIfAbsentAsync let {winners} of {callers} callers write one key at the same time; " +
                    "exactly one must win.");
            }

            var winner = "caller-" + Array.IndexOf(results, true);
            if (await store.GetAsync(key, ct) != winner)
            {
                throw new PluginContractViolationException(
                    "SetIfAbsentAsync kept a value other than the one the winner wrote.");
            }

            if (await store.SetIfAbsentAsync(key, "late", null, ct))
            {
                throw new PluginContractViolationException("SetIfAbsentAsync wrote a key that already existed.");
            }

            if (await store.GetAsync(key, ct) != winner)
            {
                throw new PluginContractViolationException(
                    "SetIfAbsentAsync changed the value of a key that already existed.");
            }
        }
        finally
        {
            await store.RemoveAsync(key, CancellationToken.None);
        }
    }

    /// <summary>
    /// Writes a value with a time to live, lets the time pass and asserts that the key then reads as absent from
    /// <see cref="IPluginStore.GetAsync"/> and from <see cref="IPluginStoreAdvanced.ListKeysAsync"/>, and can be
    /// written again with <see cref="IPluginStoreAdvanced.SetIfAbsentAsync"/>. Before it passes the key must still be
    /// there.
    /// </summary>
    /// <param name="store">The store under test.</param>
    /// <param name="advance">
    /// Lets the given time pass for the store: moves the fake clock of an in memory store, or waits for a real one.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> or <paramref name="advance"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">
    /// The key was gone too early, or was still readable, listed or taken after it expired.
    /// </exception>
    public static async Task AssertExpiredKeyReadsAsAbsentAsync(
        IPluginStoreAdvanced store,
        Func<TimeSpan, Task> advance,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(advance);

        var key = Prefix + "expiring";
        var timeToLive = ShortTimeToLive;
        try
        {
            await store.SetAsync(key, "value", timeToLive, ct);
            await advance(HalfOfShortTimeToLive);
            if (await store.GetAsync(key, ct) != "value")
            {
                throw new PluginContractViolationException(
                    "A value with a time to live was gone before the time to live passed.");
            }

            await advance(PastShortTimeToLive);
            if (await store.GetAsync(key, ct) is not null)
            {
                throw new PluginContractViolationException("An expired key was still readable.");
            }

            if ((await store.ListKeysAsync(Prefix, PluginStoreLimits.MaxListLimit, ct)).Contains(key))
            {
                throw new PluginContractViolationException("An expired key was still listed.");
            }

            if (!await store.SetIfAbsentAsync(key, "again", null, ct))
            {
                throw new PluginContractViolationException("SetIfAbsentAsync refused to write a key that had expired.");
            }
        }
        finally
        {
            await store.RemoveAsync(key, CancellationToken.None);
        }
    }

    /// <summary>
    /// Writes a value with a time to live, then writes the same key with a plain <see cref="IPluginStore.SetAsync"/>,
    /// lets the time pass and asserts that the key is still there: a plain write replaces the value and clears the
    /// expiry, so the key lives until it is removed.
    /// </summary>
    /// <param name="store">The store under test.</param>
    /// <param name="advance">Lets the given time pass for the store, as for
    /// <see cref="AssertExpiredKeyReadsAsAbsentAsync"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> or <paramref name="advance"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">The plain write kept the old expiry, so the key
    /// expired.</exception>
    public static async Task AssertPlainSetClearsTimeToLiveAsync(
        IPluginStoreAdvanced store,
        Func<TimeSpan, Task> advance,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(advance);

        var key = Prefix + "plain";
        try
        {
            await store.SetAsync(key, "first", ShortTimeToLive, ct);
            await store.SetAsync(key, "second", ct);
            await advance(PastShortTimeToLive);
            if (await store.GetAsync(key, ct) != "second")
            {
                throw new PluginContractViolationException(
                    "A plain SetAsync kept the time to live of the value it replaced; it must clear it.");
            }
        }
        finally
        {
            await store.RemoveAsync(key, CancellationToken.None);
        }
    }

    /// <summary>
    /// Asserts the bounds of a time to live on both writes that take one: zero, a negative value and a value above
    /// <see cref="PluginStoreLimits.MaxTimeToLive"/> are refused with <see cref="ArgumentOutOfRangeException"/> and
    /// write nothing, and <see cref="PluginStoreLimits.MaxTimeToLive"/> itself is accepted.
    /// </summary>
    /// <param name="store">The store under test.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">
    /// A bad time to live was accepted or written, or the largest one was refused.
    /// </exception>
    public static async Task AssertTimeToLiveBoundsAsync(IPluginStoreAdvanced store, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(store);

        var key = Prefix + "bounds";
        TimeSpan[] refused =
        [
            TimeSpan.Zero,
            -OneSecond,
            PluginStoreLimits.MaxTimeToLive.Add(OneSecond),
        ];
        try
        {
            foreach (var timeToLive in refused)
            {
                await ExpectOutOfRangeAsync("SetAsync", () => store.SetAsync(key, "value", timeToLive, ct));
                await ExpectOutOfRangeAsync(
                    "SetIfAbsentAsync", () => store.SetIfAbsentAsync(key, "value", timeToLive, ct));
                if (await store.GetAsync(key, ct) is not null)
                {
                    throw new PluginContractViolationException("A refused time to live still wrote the value.");
                }
            }

            if (!await store.SetIfAbsentAsync(key, "value", PluginStoreLimits.MaxTimeToLive, ct))
            {
                throw new PluginContractViolationException("The largest time to live, MaxTimeToLive, was refused.");
            }
        }
        finally
        {
            await store.RemoveAsync(key, CancellationToken.None);
        }
    }

    /// <summary>
    /// Writes keys under two prefixes and asserts that <see cref="IPluginStoreAdvanced.ListKeysAsync"/> returns only
    /// the keys under the asked prefix, in ascending ordinal order, never more than the limit, and not a removed key.
    /// </summary>
    /// <param name="store">The store under test.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> is <see langword="null"/>.</exception>
    /// <exception cref="PluginContractViolationException">
    /// The list holds a key of another prefix, is not in order, is longer than the limit, or holds a removed key.
    /// </exception>
    public static async Task AssertListKeysHonoursPrefixAndLimitAsync(
        IPluginStoreAdvanced store,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(store);

        string[] keys = [Prefix + "list:b", Prefix + "list:a", Prefix + "list:c", Prefix + "other:a"];
        try
        {
            foreach (var key in keys)
            {
                await store.SetAsync(key, "value", ct);
            }

            var listed = await store.ListKeysAsync(Prefix + "list:", PluginStoreLimits.MaxListLimit, ct);
            string[] expected = [Prefix + "list:a", Prefix + "list:b", Prefix + "list:c"];
            if (!listed.SequenceEqual(expected, StringComparer.Ordinal))
            {
                throw new PluginContractViolationException(
                    "ListKeysAsync did not return exactly the keys under the prefix in ascending ordinal order.");
            }

            var limited = await store.ListKeysAsync(Prefix + "list:", 2, ct);
            if (limited.Count != 2)
            {
                throw new PluginContractViolationException(
                    "ListKeysAsync returned more keys than the limit it was given.");
            }

            await store.RemoveAsync(Prefix + "list:b", ct);
            var remaining = await store.ListKeysAsync(Prefix + "list:", PluginStoreLimits.MaxListLimit, ct);
            if (remaining.Contains(Prefix + "list:b"))
            {
                throw new PluginContractViolationException("ListKeysAsync returned a key that was removed.");
            }
        }
        finally
        {
            foreach (var key in keys)
            {
                await store.RemoveAsync(key, CancellationToken.None);
            }
        }
    }

    /// <summary>Runs a write and asserts that it throws <see cref="ArgumentOutOfRangeException"/>.</summary>
    /// <param name="operation">The name of the write, for the message.</param>
    /// <param name="write">The write.</param>
    /// <returns>A task that completes when the check passed.</returns>
    /// <exception cref="PluginContractViolationException">The write did not throw, or threw another
    /// exception.</exception>
    private static async Task ExpectOutOfRangeAsync(string operation, Func<Task> write)
    {
        try
        {
            await write();
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new PluginContractViolationException(
                $"{operation} threw {ex.GetType().Name} for a time to live out of range; it must throw " +
                "ArgumentOutOfRangeException.");
        }

        throw new PluginContractViolationException(
            $"{operation} accepted a time to live out of range; zero, negative and above MaxTimeToLive are refused.");
    }
}
