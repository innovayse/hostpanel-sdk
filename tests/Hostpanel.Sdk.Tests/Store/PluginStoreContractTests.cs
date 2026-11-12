using Hostpanel.Sdk.Plugins.Store;
using Hostpanel.Sdk.Plugins.StoredCredentials;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Store;

/// <summary>Tests of <see cref="IPluginStoreAdvanced"/> and of <see cref="PluginStoreContract"/>.</summary>
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

    /// <summary>The limits are pinned: a host never allows more, so a change is a deliberate decision.</summary>
    [Fact]
    public void PluginStoreLimits_Values_ArePinned()
    {
        PluginStoreLimits.MaxListLimit.Should().Be(200);
        PluginStoreLimits.MaxTimeToLive.Should().Be(TimeSpan.FromDays(30));
    }

    /// <summary>A store that keeps its promises passes every check.</summary>
    [Fact]
    public async Task WellBehavedStore_PassesEveryCheck()
    {
        var store = new InMemoryAdvancedStore();

        await PluginStoreContract.AssertSetIfAbsentIsAtomicAsync(store);
        await PluginStoreContract.AssertExpiredKeyReadsAsAbsentAsync(store, store.AdvanceAsync);
        await PluginStoreContract.AssertListKeysHonoursPrefixAndLimitAsync(store);

        (await store.ListKeysAsync(string.Empty, PluginStoreLimits.MaxListLimit, CancellationToken.None))
            .Should().BeEmpty("the checks remove what they wrote");
    }

    /// <summary>A conditional write that checks and writes in two steps lets several callers win and is
    /// rejected.</summary>
    [Fact]
    public async Task SetIfAbsent_NotAtomic_Throws()
    {
        var store = new InMemoryAdvancedStore { NonAtomic = true };

        var run = () => PluginStoreContract.AssertSetIfAbsentIsAtomicAsync(store);

        await AssertViolationAsync(run, "exactly one");
    }

    /// <summary>A store that never expires a key is rejected.</summary>
    [Fact]
    public async Task Expiry_Ignored_Throws()
    {
        var store = new InMemoryAdvancedStore { IgnoreExpiry = true };

        var run = () => PluginStoreContract.AssertExpiredKeyReadsAsAbsentAsync(store, store.AdvanceAsync);

        await AssertViolationAsync(run, "still readable");
    }

    /// <summary>A store that drops a value before its time to live passed is rejected.</summary>
    [Fact]
    public async Task Expiry_TooEarly_Throws()
    {
        var store = new InMemoryAdvancedStore();

        var run = () => PluginStoreContract.AssertExpiredKeyReadsAsAbsentAsync(
            store, elapsed => store.AdvanceAsync(elapsed + TimeSpan.FromMinutes(5)));

        await AssertViolationAsync(run, "gone before");
    }

    /// <summary>A listing that ignores the prefix is rejected.</summary>
    [Fact]
    public async Task ListKeys_IgnoresPrefix_Throws()
    {
        var store = new InMemoryAdvancedStore { ListEverything = true };

        var run = () => PluginStoreContract.AssertListKeysHonoursPrefixAndLimitAsync(store);

        await AssertViolationAsync(run, "under the prefix");
    }

    /// <summary>A listing that ignores the limit is rejected.</summary>
    [Fact]
    public async Task ListKeys_IgnoresLimit_Throws()
    {
        var store = new InMemoryAdvancedStore { IgnoreLimit = true };

        var run = () => PluginStoreContract.AssertListKeysHonoursPrefixAndLimitAsync(store);

        await AssertViolationAsync(run, "more keys than the limit");
    }

    /// <summary>A mistake of the caller is reported when the helper is called with nothing to check or too few
    /// callers.</summary>
    [Fact]
    public async Task Helpers_BadArguments_Throw()
    {
        var store = new InMemoryAdvancedStore();

        var noStore = () => PluginStoreContract.AssertSetIfAbsentIsAtomicAsync(null!);
        var oneCaller = () => PluginStoreContract.AssertSetIfAbsentIsAtomicAsync(store, callers: 1);
        var noClock = () => PluginStoreContract.AssertExpiredKeyReadsAsAbsentAsync(store, null!);

        await noStore.Should().ThrowAsync<ArgumentNullException>();
        await oneCaller.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await noClock.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>A time to live of zero is refused by the store, as the interface documents.</summary>
    [Fact]
    public async Task Set_ZeroTimeToLive_Throws()
    {
        var store = new InMemoryAdvancedStore();

        var write = () => store.SetAsync("k", "v", TimeSpan.Zero, CancellationToken.None);

        await write.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>Asserts that a helper call throws a violation whose message contains the given text.</summary>
    /// <param name="run">The helper call.</param>
    /// <param name="expected">The text the message must contain.</param>
    /// <returns>A task that completes when the check passed.</returns>
    private static async Task AssertViolationAsync(Func<Task> run, string expected)
    {
        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain(expected);
    }
}
