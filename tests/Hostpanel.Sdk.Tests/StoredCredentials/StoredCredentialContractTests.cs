using Hostpanel.Sdk.Plugins.Payment;
using Hostpanel.Sdk.Plugins.StoredCredentials;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.StoredCredentials;

/// <summary>Tests of <see cref="StoredCredentialContract.AssertCompleteSaveMethodIsIdempotentAsync"/>.</summary>
public sealed class StoredCredentialContractTests
{
    /// <summary>A saved method every call answers identically.</summary>
    private static readonly SavedMethodResult Saved = new(true, new SavedMethod("pm_1", "visa", "4242", 12, 2030, true));

    /// <summary>A plugin that answers the same saved method every time passes, and the helper calls it as often as asked.</summary>
    [Fact]
    public async Task Assert_SameResultEveryCall_Passes()
    {
        var plugin = new ScriptedStoredCredentialPlugin(_ => Saved);

        var result = await StoredCredentialContract.AssertCompleteSaveMethodIsIdempotentAsync(plugin, "s1", calls: 4);

        result.Should().Be(Saved);
        plugin.Calls.Should().Be(4);
    }

    /// <summary>An abandoned session that answers Saved = false every time passes.</summary>
    [Fact]
    public async Task Assert_AbandonedSession_PassesWithNotSaved()
    {
        var plugin = new ScriptedStoredCredentialPlugin(_ => new SavedMethodResult(false, null));

        var result = await StoredCredentialContract.AssertCompleteSaveMethodIsIdempotentAsync(plugin, "s1");

        result.Saved.Should().BeFalse();
    }

    /// <summary>A plugin that stores a second method on a repeat, and so answers differently, is rejected.</summary>
    [Fact]
    public async Task Assert_DifferentMethodOnRepeat_Throws()
    {
        var plugin = new ScriptedStoredCredentialPlugin(call => new SavedMethodResult(true, Saved.Method! with { MethodRef = "pm_" + call }));

        var run = () => StoredCredentialContract.AssertCompleteSaveMethodIsIdempotentAsync(plugin, "s1");

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("call 2");
    }

    /// <summary>A plugin that throws on the repeat is rejected, with the exception type in the message.</summary>
    [Fact]
    public async Task Assert_ThrowsOnRepeat_Throws()
    {
        var plugin = new ScriptedStoredCredentialPlugin(call => call == 1 ? Saved : throw new InvalidOperationException("already stored"));

        var run = () => StoredCredentialContract.AssertCompleteSaveMethodIsIdempotentAsync(plugin, "s1");

        (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message.Should().Contain("InvalidOperationException");
    }

    /// <summary>A result whose Saved flag disagrees with its method is rejected.</summary>
    [Fact]
    public async Task Assert_SavedWithoutMethod_Throws()
    {
        var plugin = new ScriptedStoredCredentialPlugin(_ => new SavedMethodResult(true, null));

        var run = () => StoredCredentialContract.AssertCompleteSaveMethodIsIdempotentAsync(plugin, "s1");

        await run.Should().ThrowAsync<PluginContractViolationException>();
    }

    /// <summary>Fewer than two calls, a blank session or no plugin is a mistake in the test and is refused.</summary>
    [Fact]
    public async Task Assert_InvalidArguments_Throw()
    {
        var plugin = new ScriptedStoredCredentialPlugin(_ => Saved);

        await FluentActions.Awaiting(() => StoredCredentialContract.AssertCompleteSaveMethodIsIdempotentAsync(plugin, "s1", calls: 1))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
        await FluentActions.Awaiting(() => StoredCredentialContract.AssertCompleteSaveMethodIsIdempotentAsync(plugin, " "))
            .Should().ThrowAsync<ArgumentException>();
        await FluentActions.Awaiting(() => StoredCredentialContract.AssertCompleteSaveMethodIsIdempotentAsync(null!, "s1"))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>The stored credential interface extends the payment interface, so an implementer is always a payment plugin.</summary>
    [Fact]
    public void IStoredCredentialPlugin_ExtendsIPaymentPlugin()
    {
        typeof(IPaymentPlugin).IsAssignableFrom(typeof(IStoredCredentialPlugin)).Should().BeTrue();
    }
}
