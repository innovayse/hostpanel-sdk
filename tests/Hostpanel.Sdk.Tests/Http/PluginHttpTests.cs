using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Http;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Http;

/// <summary>
/// Tests of <see cref="PluginHttp"/>: the early refusal, the cut off, the deadlines, the final address and the wrapped
/// fault.
/// </summary>
public sealed class PluginHttpTests
{
    /// <summary>An address with a secret in its query, which no exception of the helper may repeat.</summary>
    private const string SecretUrl = "https://gateway.example/pay?token=SECRET-TOKEN";

    /// <summary>A deadline long enough that only a test moving the clock reaches it.</summary>
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

    /// <summary>The clock the helper under test runs on.</summary>
    private readonly ManualTimeProvider _time = new();

    /// <summary>A body within the limit is read whole.</summary>
    [Fact]
    public async Task ReadBounded_WithinLimit_ReturnsTheBody()
    {
        using var response = Response(new ByteArrayContent([1, 2, 3, 4]));

        var body = await Http().ReadBoundedAsync(response, 4, Deadline, CancellationToken.None);

        body.Should().Equal(1, 2, 3, 4);
    }

    /// <summary>A response without content reads as an empty body.</summary>
    [Fact]
    public async Task ReadBounded_NoContent_ReturnsEmpty()
    {
        using var response = Response(new ByteArrayContent([]));

        var body = await Http().ReadBoundedAsync(response, 4, Deadline, CancellationToken.None);

        body.Should().BeEmpty();
    }

    /// <summary>A declared length over the limit is refused before a byte of the body is read.</summary>
    [Fact]
    public async Task ReadBounded_DeclaredLengthOverLimit_RefusesWithoutReading()
    {
        var content = new UnreadableContent { Declared = 1000 };
        using var response = Response(content);

        var read = () => Http().ReadBoundedAsync(response, 100, Deadline, CancellationToken.None);

        (await read.Should().ThrowAsync<PluginHttpException>()).Which.Fault.Should().Be(PluginHttpFault.TooLarge);
        content.WasRead.Should().BeFalse();
    }

    /// <summary>A body with no declared length is cut off at the limit and refused.</summary>
    [Fact]
    public async Task ReadBounded_NoLengthOverLimit_StopsAtTheLimit()
    {
        var source = new CountingStream(1_000_000);
        using var response = Response(new StreamContent(source));

        var read = () => Http().ReadBoundedAsync(response, 10, Deadline, CancellationToken.None);

        (await read.Should().ThrowAsync<PluginHttpException>()).Which.Fault.Should().Be(PluginHttpFault.TooLarge);
        source.BytesRead.Should().BeLessThan(100_000, "the read stops soon after the limit");
    }

    /// <summary>A body that arrives too slowly is cut off when the deadline passes, even after some bytes
    /// came.</summary>
    [Fact]
    public async Task ReadBounded_SlowBody_TimesOutAtTheDeadline()
    {
        using var response = Response(new StreamContent(new HangingStream()));

        var read = Http().ReadBoundedAsync(response, 100, Deadline, CancellationToken.None);
        await WaitForTimerAsync();
        _time.Advance(Deadline + TimeSpan.FromSeconds(1));

        (await ((Func<Task>)(() => read)).Should().ThrowAsync<PluginHttpException>())
            .Which.Fault.Should().Be(PluginHttpFault.TimedOut);
    }

    /// <summary>A body that finishes before the deadline is not affected by it.</summary>
    [Fact]
    public async Task ReadBounded_BeforeTheDeadline_Completes()
    {
        using var response = Response(new ByteArrayContent([9]));

        var body = await Http().ReadBoundedAsync(response, 4, Deadline, CancellationToken.None);
        _time.Advance(Deadline * 2);

        body.Should().Equal(9);
    }

    /// <summary>A read the caller cancels stays a cancellation and is not a fault.</summary>
    [Fact]
    public async Task ReadBounded_CallerCancels_StaysACancellation()
    {
        using var response = Response(new StreamContent(new HangingStream()));
        using var cancel = new CancellationTokenSource();

        var read = Http().ReadBoundedAsync(response, 100, Deadline, cancel.Token);
        await WaitForTimerAsync();
        await cancel.CancelAsync();

        await ((Func<Task>)(() => read)).Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>A limit below 1 and a deadline of zero are mistakes of the caller.</summary>
    [Fact]
    public async Task ReadBounded_BadArguments_Throw()
    {
        using var response = Response(new ByteArrayContent([1]));

        var noLimit = () => Http().ReadBoundedAsync(response, 0, Deadline, CancellationToken.None);
        var noDeadline = () => Http().ReadBoundedAsync(response, 1, TimeSpan.Zero, CancellationToken.None);

        await noLimit.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await noDeadline.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>A response from the expected scheme, host and port passes, whatever the case of the name.</summary>
    /// <param name="expected">The address the request was built for.</param>
    /// <param name="actual">The address of the request behind the response.</param>
    [Theory]
    [InlineData("https://gateway.example/a", "https://Gateway.Example/b?x=1")]
    [InlineData("https://gateway.example:8443/a", "https://gateway.example:8443/b")]
    [InlineData("https://[::1]:8443/a", "https://[::1]:8443/b")]
    public void EnsureFinalHost_SameOrigin_Passes(string expected, string actual)
    {
        using var response = Response(new ByteArrayContent([]), actual);

        var check = () => PluginHttp.EnsureFinalHost(response, new Uri(expected));

        check.Should().NotThrow();
    }

    /// <summary>
    /// A response that ended on another host, port or scheme, as after a redirect, is refused without repeating any
    /// address.
    /// </summary>
    /// <param name="actual">The address of the request behind the response.</param>
    [Theory]
    [InlineData("https://evil.example/a")]
    [InlineData("https://gateway.example:8443/a")]
    [InlineData("http://gateway.example/a")]
    [InlineData("https://[::2]/a")]
    public void EnsureFinalHost_OtherOrigin_Throws(string actual)
    {
        using var response = Response(new ByteArrayContent([]), actual);

        var check = () => PluginHttp.EnsureFinalHost(response, new Uri("https://gateway.example/a"));

        var thrown = check.Should().Throw<PluginHttpException>().Which;
        thrown.Fault.Should().Be(PluginHttpFault.UnexpectedHost);
        thrown.Message.Should().NotContain("evil.example").And.NotContain("gateway.example");
    }

    /// <summary>A response with no request address cannot be trusted, and a relative expected address is a
    /// mistake.</summary>
    [Fact]
    public void EnsureFinalHost_NoAddress_Throws()
    {
        using var response = new HttpResponseMessage();

        var noRequest = () => PluginHttp.EnsureFinalHost(response, new Uri("https://gateway.example/a"));
        var relative = () => PluginHttp.EnsureFinalHost(response, new Uri("/a", UriKind.Relative));

        noRequest.Should().Throw<PluginHttpException>();
        relative.Should().Throw<ArgumentException>();
    }

    /// <summary>A transport fault becomes one exception with no inner exception and no address or query in
    /// it.</summary>
    [Fact]
    public async Task Send_TransportFault_IsWrappedWithoutTheAddress()
    {
        var inner = new IOException("connection reset by " + SecretUrl);
        using var client = new HttpClient(new FaultHandler(new HttpRequestException(SecretUrl, inner)));
        using var request = new HttpRequestMessage(HttpMethod.Get, SecretUrl);

        var send = () => Http().SendAsync(client, request, Deadline, CancellationToken.None);

        var thrown = (await send.Should().ThrowAsync<PluginHttpException>()).Which;
        thrown.Fault.Should().Be(PluginHttpFault.Transport);
        thrown.InnerException.Should().BeNull();
        thrown.Message.Should().NotContain("SECRET-TOKEN").And.NotContain("gateway.example");
        thrown.ToString().Should().NotContain("SECRET-TOKEN");
    }

    /// <summary>A request that does not answer before the deadline is a timeout, not a transport fault.</summary>
    [Fact]
    public async Task Send_NoAnswerBeforeTheDeadline_TimesOut()
    {
        using var client = new HttpClient(new HangingHandler());
        using var request = new HttpRequestMessage(HttpMethod.Get, SecretUrl);

        var send = Http().SendAsync(client, request, Deadline, CancellationToken.None);
        await WaitForTimerAsync();
        _time.Advance(Deadline + TimeSpan.FromSeconds(1));

        var thrown = (await ((Func<Task>)(() => send)).Should().ThrowAsync<PluginHttpException>()).Which;
        thrown.Fault.Should().Be(PluginHttpFault.TimedOut);
        thrown.InnerException.Should().BeNull();
        thrown.Message.Should().NotContain("SECRET-TOKEN");
    }

    /// <summary>A cancellation the caller asked for stays a cancellation.</summary>
    [Fact]
    public async Task Send_CallerCancels_StaysACancellation()
    {
        using var client = new HttpClient(new HangingHandler());
        using var request = new HttpRequestMessage(HttpMethod.Get, SecretUrl);
        using var cancel = new CancellationTokenSource();

        var send = Http().SendAsync(client, request, Deadline, cancel.Token);
        await WaitForTimerAsync();
        await cancel.CancelAsync();

        await ((Func<Task>)(() => send)).Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>A successful request returns the response with its body still unread, ready for the bounded
    /// read.</summary>
    [Fact]
    public async Task Send_Success_ReturnsTheResponse()
    {
        using var client = new HttpClient(new FaultHandler(null));
        using var request = new HttpRequestMessage(HttpMethod.Get, SecretUrl);

        using var response = await Http().SendAsync(client, request, Deadline, CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        PluginHttp.EnsureFinalHost(response, new Uri("https://gateway.example/"));
    }

    /// <summary>Missing arguments and a deadline of zero are mistakes of the caller.</summary>
    [Fact]
    public async Task Helpers_BadArguments_Throw()
    {
        using var client = new HttpClient(new FaultHandler(null));
        using var request = new HttpRequestMessage(HttpMethod.Get, SecretUrl);

        var noClient = () => Http().SendAsync(null!, request, Deadline, CancellationToken.None);
        var noDeadline = () => Http().SendAsync(client, request, TimeSpan.Zero, CancellationToken.None);
        var noResponse = () => Http().ReadBoundedAsync(null!, 1, Deadline, CancellationToken.None);
        var noClock = () => new PluginHttp(null!);

        await noClient.Should().ThrowAsync<ArgumentNullException>();
        await noDeadline.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await noResponse.Should().ThrowAsync<ArgumentNullException>();
        noClock.Should().Throw<ArgumentNullException>();
    }

    /// <summary>The fault values start at 1 and keep their numbers.</summary>
    [Fact]
    public void PluginHttpFault_Values_ArePinned()
    {
        Enum.GetValues<PluginHttpFault>().Select(value => (int)value).Should().Equal(1, 2, 3, 4);
        Enum.IsDefined((PluginHttpFault)0).Should().BeFalse();
    }

    /// <summary>Builds the helper under test on the manual clock.</summary>
    /// <returns>The helper.</returns>
    private PluginHttp Http()
    {
        return new PluginHttp(_time);
    }

    /// <summary>
    /// Waits, without sleeping, until the operation under test has started its deadline timer. The wait is bounded, so a
    /// test whose operation never starts a timer fails instead of hanging.
    /// </summary>
    /// <returns>A task that completes when a timer is waiting.</returns>
    /// <exception cref="TimeoutException">No timer was started within the bound.</exception>
    private Task WaitForTimerAsync()
    {
        return SpinUntilTimerAsync().WaitAsync(TimeSpan.FromSeconds(10));
    }

    /// <summary>Yields until a timer is waiting.</summary>
    /// <returns>A task that completes when a timer is waiting.</returns>
    private async Task SpinUntilTimerAsync()
    {
        while (_time.PendingTimers == 0)
        {
            await Task.Yield();
        }
    }

    /// <summary>Builds a response with a body and the address of the request that produced it.</summary>
    /// <param name="content">The body.</param>
    /// <param name="requestUri">The address of the request, as after the last redirect.</param>
    /// <returns>The response.</returns>
    private static HttpResponseMessage Response(HttpContent content, string requestUri = SecretUrl)
    {
        return new HttpResponseMessage
        {
            Content = content,
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri),
        };
    }

    /// <summary>A handler that throws a given exception, or answers 200 when none is given.</summary>
    private sealed class FaultHandler : HttpMessageHandler
    {
        /// <summary>The exception to throw, or <see langword="null"/> to answer.</summary>
        private readonly Exception? _fault;

        /// <summary>Initializes a new instance of the <see cref="FaultHandler"/> class.</summary>
        /// <param name="fault">The exception to throw, or <see langword="null"/> to answer 200.</param>
        public FaultHandler(Exception? fault)
        {
            _fault = fault;
        }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            return _fault is null
                ? Task.FromResult(
                    new HttpResponseMessage { RequestMessage = request, Content = new ByteArrayContent([]) })
                : Task.FromException<HttpResponseMessage>(_fault);
        }
    }

    /// <summary>A handler that never answers until its token is cancelled.</summary>
    private sealed class HangingHandler : HttpMessageHandler
    {
        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage();
        }
    }

    /// <summary>A body that claims a length and fails if anyone reads it.</summary>
    private sealed class UnreadableContent : HttpContent
    {
        /// <summary>Sets the length the body declares.</summary>
        public long Declared
        {
            init => Headers.ContentLength = value;
        }

        /// <summary>Gets a value indicating whether the body was read.</summary>
        public bool WasRead { get; private set; }

        /// <inheritdoc />
        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext? context)
        {
            WasRead = true;
            throw new InvalidOperationException("The body must not be read.");
        }

        /// <inheritdoc />
        protected override bool TryComputeLength(out long length)
        {
            length = Headers.ContentLength ?? 0;
            return true;
        }
    }

    /// <summary>A stream that gives three bytes and then never gives another until its token is cancelled.</summary>
    private sealed class HangingStream : Stream
    {
        /// <summary>Whether the three bytes were given.</summary>
        private bool _gave;

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            if (!_gave)
            {
                _gave = true;
                buffer.Span[..3].Fill(1);
                return 3;
            }

            await Task.Delay(Timeout.Infinite, ct);
            return 0;
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>A stream of zero bytes that counts how many were taken from it.</summary>
    private sealed class CountingStream : Stream
    {
        /// <summary>The number of bytes the stream can still give.</summary>
        private long _left;

        /// <summary>Initializes a new instance of the <see cref="CountingStream"/> class.</summary>
        /// <param name="length">The total number of bytes the stream gives.</param>
        public CountingStream(long length)
        {
            _left = length;
        }

        /// <summary>Gets how many bytes were taken.</summary>
        public long BytesRead { get; private set; }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            var take = (int)Math.Min(count, _left);
            _left -= take;
            BytesRead += take;
            return take;
        }

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
