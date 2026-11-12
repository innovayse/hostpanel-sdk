using Hostpanel.Sdk.Plugins.Http;
using Hostpanel.Sdk.Plugins.StoredCredentials;

namespace Hostpanel.Sdk.Tests.Http;

/// <summary>Tests of <see cref="PluginHttpContract"/>: a client that does not follow redirects passes, one that does is rejected.</summary>
public sealed class PluginHttpContractTests
{
    /// <summary>A client that returns the 3xx answer and makes no second request passes.</summary>
    [Fact]
    public async Task AssertRedirectIsNotFollowed_ClientThatDoesNotFollow_Passes()
    {
        var run = () => PluginHttpContract.AssertRedirectIsNotFollowedAsync(
            () => new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }));

        await run.Should().NotThrowAsync();
    }

    /// <summary>A client that follows redirects, the default of the framework, is rejected.</summary>
    [Fact]
    public async Task AssertRedirectIsNotFollowed_ClientThatFollows_Throws()
    {
        var run = () => PluginHttpContract.AssertRedirectIsNotFollowedAsync(
            () => new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }));

        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain("followed the redirect");
    }

    /// <summary>
    /// A client that cannot reach the listener is reported as a check that could not run, not as a client that did not
    /// return the redirect.
    /// </summary>
    [Fact]
    public async Task AssertRedirectIsNotFollowed_ListenerNotReachable_SaysTheCheckCouldNotRun()
    {
        var run = () => PluginHttpContract.AssertRedirectIsNotFollowedAsync(
            () => new HttpClient(new BlockedHandler()));

        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain("could not reach the local listener").And.Contain("nothing about redirects");
    }

    /// <summary>A missing client factory is a mistake of the caller, reported when the helper is called.</summary>
    [Fact]
    public async Task AssertRedirectIsNotFollowed_NullFactory_Throws()
    {
        var run = () => PluginHttpContract.AssertRedirectIsNotFollowedAsync(null!);

        await run.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>A handler that fails every request the way a blocked address does.</summary>
    private sealed class BlockedHandler : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            return Task.FromException<HttpResponseMessage>(new HttpRequestException("blocked"));
        }
    }
}
