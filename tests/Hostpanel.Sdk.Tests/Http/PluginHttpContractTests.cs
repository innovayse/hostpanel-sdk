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

    /// <summary>A client that follows the redirect over and over is reported as one that kept following it.</summary>
    [Fact]
    public async Task AssertRedirectIsNotFollowed_KeepsFollowing_SaysItKeptFollowing()
    {
        var run = () => PluginHttpContract.AssertRedirectIsNotFollowedAsync(
            () => new HttpClient(new FailingAfterHandler(requestsBeforeFailing: 3)));

        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain("kept following");
    }

    /// <summary>
    /// A client that reached the listener but failed on the answer is reported as such, not as a blocked address.
    /// </summary>
    [Fact]
    public async Task AssertRedirectIsNotFollowed_FailsAfterOneRequest_SaysItReachedTheListener()
    {
        var run = () => PluginHttpContract.AssertRedirectIsNotFollowedAsync(
            () => new HttpClient(new FailingAfterHandler(requestsBeforeFailing: 1)));

        var message = (await run.Should().ThrowAsync<PluginContractViolationException>()).Which.Message;
        message.Should().Contain("reached the listener but failed");
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

    /// <summary>
    /// A handler that sends a number of requests to the real listener without following anything, and then fails the
    /// way a client does when redirects loop.
    /// </summary>
    private sealed class FailingAfterHandler : DelegatingHandler
    {
        /// <summary>How many requests are sent before the failure.</summary>
        private readonly int _requestsBeforeFailing;

        /// <summary>Initializes a new instance of the <see cref="FailingAfterHandler"/> class.</summary>
        /// <param name="requestsBeforeFailing">How many requests are sent before the failure.</param>
        public FailingAfterHandler(int requestsBeforeFailing)
            : base(new HttpClientHandler { AllowAutoRedirect = false })
        {
            _requestsBeforeFailing = requestsBeforeFailing;
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            for (var sent = 0; sent < _requestsBeforeFailing; sent++)
            {
                using var response = await base.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, request.RequestUri), ct);
            }

            throw new HttpRequestException("too many redirects");
        }
    }
}
