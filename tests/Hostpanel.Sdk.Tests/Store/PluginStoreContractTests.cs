using Hostpanel.Sdk.Plugins.Store;
using Hostpanel.Sdk.Plugins.StoredCredentials;
using Hostpanel.Sdk.Tests.Fakes;
using static Hostpanel.Sdk.Tests.Fakes.ContractViolationAsserts;

namespace Hostpanel.Sdk.Tests.Store;

/// <summary>
/// Tests of <see cref="IPluginStoreAdvanced"/>, of <see cref="InMemoryPluginStore"/> and of
/// <see cref="PluginStoreContract"/>.
/// </summary>
public sealed class PluginStoreContractTests
{
    /// <summary>The advanced store is a plugin store, so a plugin that has the old one can test for the new
    /// one.</summary>
    [Fact]
    public void AdvancedStore_ExtendsThePluginStore()
    {
        typeof(IPluginStore).IsAssignableFrom(typeof(IPluginStoreAdvanced)).Should().BeTrue();
        new StubPluginStore().Should().NotBeAssignableTo<IPluginStoreAdvanced>();
    }

    /// <summary>The limits are pinned: a host must not allow more, so a change is a deliberate decision.</summary>
    [Fact]
    public void PluginStoreLimits_Values_ArePinned()
    {
        PluginStoreLimits.MaxListLimit.Should().Be(200);
        PluginStoreLimits.MaxKeys.Should().Be(1000);
        PluginStoreLimits.MaxTimeToLive.Should().Be(TimeSpan.FromDays(30));
    }

    /// <summary>The in memory store keeps its promises and passes every check.</summary>
    [Fact]
    public async Task InMemoryStore_PassesEveryCheck()
    {
        var store = new InMemoryPluginStore();

        await PluginStoreContract.AssertSetIfAbsentIsAtomicAsync(store);
        await PluginStoreContract.AssertExpiredKeyReadsAsAbsentAsync(store, store.AdvanceAsync);
        await PluginStoreContract.AssertPlainSetClearsTimeToLiveAsync(store, store.AdvanceAsync);
        await PluginStoreContract.AssertTimeToLiveBoundsAsync(store);
        await PluginStoreContract.AssertListKeysHonoursPrefixAndLimitAsync(store);

        (await store.ListKeysAsync(string.Empty, PluginStoreLimits.MaxListLimit, CancellationToken.None))
            .Should().BeEmpty("the checks remove what they wrote");
    }

    /// <summary>A conditional write that checks and writes in two steps lets several callers win.</summary>
    [Fact]
    public async Task SetIfAbsent_NotAtomic_Throws()
    {
        var store = new BreakableStore { NonAtomic = true };

        var run = () => PluginStoreContract.AssertSetIfAbsentIsAtomicAsync(store);

        await AssertViolationAsync(run, "exactly one");
    }

    /// <summary>A store that never expires a key is rejected.</summary>
    [Fact]
    public async Task Expiry_Ignored_Throws()
    {
        var store = new BreakableStore { IgnoreExpiry = true };

        var run = () => PluginStoreContract.AssertExpiredKeyReadsAsAbsentAsync(store, store.AdvanceAsync);

        await AssertViolationAsync(run, "still readable");
    }

    /// <summary>A store that drops a value before its time to live passed is rejected.</summary>
    [Fact]
    public async Task Expiry_TooEarly_Throws()
    {
        var store = new InMemoryPluginStore();

        var run = () => PluginStoreContract.AssertExpiredKeyReadsAsAbsentAsync(
            store, elapsed => store.AdvanceAsync(elapsed + TimeSpan.FromMinutes(5)));

        await AssertViolationAsync(run, "gone before");
    }

    /// <summary>A plain write that keeps the expiry of the value it replaces is rejected.</summary>
    [Fact]
    public async Task PlainSet_KeepsTimeToLive_Throws()
    {
        var store = new BreakableStore { KeepTimeToLive = true };

        var run = () => PluginStoreContract.AssertPlainSetClearsTimeToLiveAsync(store, store.AdvanceAsync);

        await AssertViolationAsync(run, "kept the time to live");
    }

    /// <summary>A store that accepts a zero, negative or oversized time to live is rejected.</summary>
    [Fact]
    public async Task TimeToLive_AnyAccepted_Throws()
    {
        var store = new BreakableStore { AcceptAnyTimeToLive = true };

        var run = () => PluginStoreContract.AssertTimeToLiveBoundsAsync(store);

        await AssertViolationAsync(run, "accepted a time to live out of range");
    }

    /// <summary>A listing that ignores the prefix is rejected.</summary>
    [Fact]
    public async Task ListKeys_IgnoresPrefix_Throws()
    {
        var store = new BreakableStore { ListEverything = true };

        var run = () => PluginStoreContract.AssertListKeysHonoursPrefixAndLimitAsync(store);

        await AssertViolationAsync(run, "under the prefix");
    }

    /// <summary>A listing that ignores the limit is rejected.</summary>
    [Fact]
    public async Task ListKeys_IgnoresLimit_Throws()
    {
        var store = new BreakableStore { IgnoreLimit = true };

        var run = () => PluginStoreContract.AssertListKeysHonoursPrefixAndLimitAsync(store);

        await AssertViolationAsync(run, "more keys than the limit");
    }

    /// <summary>A new key past the quota is refused by both writes, and a key that exists still takes a new
    /// value.</summary>
    [Fact]
    public async Task Quota_NewKeyPastTheQuota_Throws()
    {
        var store = new InMemoryPluginStore(maxKeys: 2);
        await store.SetAsync("a", "1", CancellationToken.None);
        await store.SetAsync("b", "1", TimeSpan.FromMinutes(1), CancellationToken.None);

        var set = () => store.SetAsync("c", "1", TimeSpan.FromMinutes(1), CancellationToken.None);
        var setIfAbsent = () => store.SetIfAbsentAsync("c", "1", null, CancellationToken.None);

        await set.Should().ThrowAsync<PluginStoreQuotaException>();
        await setIfAbsent.Should().ThrowAsync<PluginStoreQuotaException>();
        (await store.SetIfAbsentAsync("a", "2", null, CancellationToken.None)).Should().BeFalse();
        await store.SetAsync("a", "3", CancellationToken.None);
        (await store.GetAsync("a", CancellationToken.None)).Should().Be("3");
    }

    /// <summary>An expired key does not count against the quota.</summary>
    [Fact]
    public async Task Quota_ExpiredKeysDoNotCount()
    {
        var store = new InMemoryPluginStore(maxKeys: 1);
        await store.SetAsync("a", "1", TimeSpan.FromMinutes(1), CancellationToken.None);
        await store.AdvanceAsync(TimeSpan.FromMinutes(2));

        var write = () => store.SetAsync("b", "1", CancellationToken.None);

        await write.Should().NotThrowAsync();
    }

    /// <summary>A mistake of the caller is reported when a helper is called with nothing to check.</summary>
    [Fact]
    public async Task Helpers_BadArguments_Throw()
    {
        var store = new InMemoryPluginStore();

        var noStore = () => PluginStoreContract.AssertSetIfAbsentIsAtomicAsync(null!);
        var oneCaller = () => PluginStoreContract.AssertSetIfAbsentIsAtomicAsync(store, callers: 1);
        var noClock = () => PluginStoreContract.AssertExpiredKeyReadsAsAbsentAsync(store, null!);
        var noBounds = () => PluginStoreContract.AssertTimeToLiveBoundsAsync(null!);

        await noStore.Should().ThrowAsync<ArgumentNullException>();
        await oneCaller.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await noClock.Should().ThrowAsync<ArgumentNullException>();
        await noBounds.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>Keys and prefixes follow the shared rules: allowed characters and at most 200 of them.</summary>
    /// <param name="key">The key to try.</param>
    /// <param name="valid">Whether the key is valid.</param>
    [Theory]
    [InlineData("event:123", true)]
    [InlineData("a.b-c_d:E9", true)]
    [InlineData("", false)]
    [InlineData("has space", false)]
    [InlineData("slash/key", false)]
    [InlineData("caf\u00e9", false)]
    public void PluginStoreKeys_Rules(string key, bool valid)
    {
        PluginStoreKeys.IsValidKey(key).Should().Be(valid);
        PluginStoreKeys.IsValidPrefix(key).Should().Be(valid || key.Length == 0);
    }

    /// <summary>The longest key and prefix are 200 characters.</summary>
    [Fact]
    public void PluginStoreKeys_LengthLimit()
    {
        PluginStoreKeys.IsValidKey(new string('a', PluginStoreKeys.MaxLength)).Should().BeTrue();
        PluginStoreKeys.IsValidKey(new string('a', PluginStoreKeys.MaxLength + 1)).Should().BeFalse();
        PluginStoreKeys.IsValidPrefix(new string('a', PluginStoreKeys.MaxLength + 1)).Should().BeFalse();
        PluginStoreKeys.IsValidKey(null).Should().BeFalse();
        PluginStoreKeys.IsValidPrefix(null).Should().BeFalse();
    }

    /// <summary>The in memory store refuses a bad key or prefix on every method, as the host does.</summary>
    [Fact]
    public async Task InMemoryStore_RefusesBadKeysAndPrefixes()
    {
        var store = new InMemoryPluginStore();
        var tooLong = new string('a', PluginStoreKeys.MaxLength + 1);

        var calls = new Func<Task>[]
        {
            () => store.GetAsync("bad key", CancellationToken.None),
            () => store.SetAsync("bad key", "v", CancellationToken.None),
            () => store.SetAsync(tooLong, "v", TimeSpan.FromMinutes(1), CancellationToken.None),
            () => store.RemoveAsync("", CancellationToken.None),
            () => store.SetIfAbsentAsync("a/b", "v", null, CancellationToken.None),
            () => store.ListKeysAsync("bad prefix", 10, CancellationToken.None),
            () => store.ListKeysAsync(tooLong, 10, CancellationToken.None),
        };

        foreach (var call in calls)
        {
            await call.Should().ThrowAsync<ArgumentException>();
        }

        (await store.ListKeysAsync(string.Empty, 10, CancellationToken.None)).Should().BeEmpty();
    }
}
