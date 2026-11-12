using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Http;

namespace Hostpanel.Sdk.Tests.Http;

/// <summary>Tests of <see cref="PluginHttp"/>: the early refusal, the cut off, the final host and the wrapped fault.</summary>
public sealed class PluginHttpTests
{
    /// <summary>An address with a secret in its query, which no exception of the helper may repeat.</summary>
    private const string SecretUrl = "https://gateway.example/pay?token=SECRET-TOKEN";

    /// <summary>A body within the limit is read whole.</summary>
    [Fact]
    public async Task ReadBounded_WithinLimit_ReturnsTheBody()
    {
        using var response = Response(new ByteArrayContent([1, 2, 3, 4]));

        var body = await PluginHttp.ReadBoundedAsync(response, 4, CancellationToken.None);

        body.Should().Equal(1, 2, 3, 4);
    }

    /// <summary>A response without content reads as an empty body.</summary>
    [Fact]
    public async Task ReadBounded_NoContent_ReturnsEmpty()
    {
        using var response = Response(new ByteArrayContent([]));

        var body = await PluginHttp.ReadBoundedAsync(response, 4, CancellationToken.None);

        body.Should().BeEmpty();
    }

    /// <summary>A declared length over the limit is refused before a byte of the body is read.</summary>
    [Fact]
    public async Task ReadBounded_DeclaredLengthOverLimit_RefusesWithoutReading()
    {
        var content = new UnreadableContent { Declared = 1000 };
        using var response = Response(content);

        var read = () => PluginHttp.ReadBoundedAsync(response, 100, CancellationToken.None);

        (await read.Should().ThrowAsync<PluginHttpException>()).Which.Fault.Should().Be(PluginHttpFault.TooLarge);
        content.WasRead.Should().BeFalse();
    }

    /// <summary>A body with no declared length is cut off at the limit and refused.</summary>
    [Fact]
    public async Task ReadBounded_NoLengthOverLimit_StopsAtTheLimit()
    {
        var source = new CountingStream(1_000_000);
        using var response = Response(new StreamContent(source));

        var read = () => PluginHttp.ReadBoundedAsync(response, 10, CancellationToken.None);

        (await read.Should().ThrowAsync<PluginHttpException>()).Which.Fault.Should().Be(PluginHttpFault.TooLarge);
        source.BytesRead.Should().BeLessThan(100_000, "the read stops soon after the limit and never takes the whole body");
    }

    /// <summary>A limit below 1 is a mistake of the caller.</summary>
    [Fact]
    public async Task ReadBounded_LimitBelowOne_Throws()
    {
        using var response = Response(new ByteArrayContent([1]));

        var read = () => PluginHttp.ReadBoundedAsync(response, 0, CancellationToken.None);

        await read.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    /// <summary>A response that came from the expected host passes, whatever the case of the name.</summary>
    [Fact]
    public void EnsureFinalHost_ExpectedHost_Passes()
    {
        using var response = Response(new ByteArrayContent([]), "https://Gateway.Example/a");

        var check = () => PluginHttp.EnsureFinalHost(response, "gateway.example");

        check.Should().NotThrow();
    }

    /// <summary>A response that ended on another host, as after a redirect, is refused without repeating either name.</summary>
    [Fact]
    public void EnsureFinalHost_OtherHost_Throws()
    {
        using var response = Response(new ByteArrayContent([]), "https://evil.example/a");

        var check = () => PluginHttp.EnsureFinalHost(response, "gateway.example");

        var thrown = check.Should().Throw<PluginHttpException>().Which;
        thrown.Fault.Should().Be(PluginHttpFault.UnexpectedHost);
        thrown.Message.Should().NotContain("evil.example").And.NotContain("gateway.example");
    }

    /// <summary>A response with no request address cannot be trusted and is refused.</summary>
    [Fact]
    public void EnsureFinalHost_NoRequestAddress_Throws()
    {
        using var response = new HttpResponseMessage();

        var check = () => PluginHttp.EnsureFinalHost(response, "gateway.example");

        check.Should().Throw<PluginHttpException>();
    }

    /// <summary>A transport fault becomes one exception with no inner exception and no address or query in its message.</summary>
    [Fact]
    public async Task Send_TransportFault_IsWrappedWithoutTheAddress()
    {
        var inner = new IOException("connection reset by " + SecretUrl);
        using var client = new HttpClient(new FaultHandler(new HttpRequestException(SecretUrl, inner)));
        using var request = new HttpRequestMessage(HttpMethod.Get, SecretUrl);

        var send = () => PluginHttp.SendAsync(client, request, CancellationToken.None);

        var thrown = (await send.Should().ThrowAsync<PluginHttpException>()).Which;
        thrown.Fault.Should().Be(PluginHttpFault.Transport);
        thrown.InnerException.Should().BeNull();
        thrown.Message.Should().NotContain("SECRET-TOKEN").And.NotContain("gateway.example");
        thrown.ToString().Should().NotContain("SECRET-TOKEN");
    }

    /// <summary>A timeout of the client is a transport fault, but a cancelled token stays a cancellation.</summary>
    [Fact]
    public async Task Send_TimeoutOrCancellation_AreToldApart()
    {
        using var timedOut = new HttpClient(new FaultHandler(new TaskCanceledException("timeout " + SecretUrl)));
        using var request = new HttpRequestMessage(HttpMethod.Get, SecretUrl);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        using var other = new HttpClient(new FaultHandler(new TaskCanceledException()));
        using var second = new HttpRequestMessage(HttpMethod.Get, SecretUrl);

        var timeout = () => PluginHttp.SendAsync(timedOut, request, CancellationToken.None);
        var cancel = () => PluginHttp.SendAsync(other, second, cancelled.Token);

        (await timeout.Should().ThrowAsync<PluginHttpException>()).Which.Fault.Should().Be(PluginHttpFault.Transport);
        await cancel.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>A successful request returns the response with its body still unread, ready for the bounded read.</summary>
    [Fact]
    public async Task Send_Success_ReturnsTheResponse()
    {
        using var client = new HttpClient(new FaultHandler(null));
        using var request = new HttpRequestMessage(HttpMethod.Get, SecretUrl);

        using var response = await PluginHttp.SendAsync(client, request, CancellationToken.None);

        response.IsSuccessStatusCode.Should().BeTrue();
        PluginHttp.EnsureFinalHost(response, "gateway.example");
    }

    /// <summary>Missing arguments are a mistake of the caller.</summary>
    [Fact]
    public async Task Helpers_NullArguments_Throw()
    {
        var noClient = () => PluginHttp.SendAsync(null!, new HttpRequestMessage(), CancellationToken.None);
        var noResponse = () => PluginHttp.ReadBoundedAsync(null!, 1, CancellationToken.None);

        await noClient.Should().ThrowAsync<ArgumentNullException>();
        await noResponse.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>The fault values start at 1 and keep their numbers.</summary>
    [Fact]
    public void PluginHttpFault_Values_ArePinned()
    {
        Enum.GetValues<PluginHttpFault>().Select(value => (int)value).Should().Equal(1, 2, 3);
        Enum.IsDefined((PluginHttpFault)0).Should().BeFalse();
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
                ? Task.FromResult(new HttpResponseMessage { RequestMessage = request, Content = new ByteArrayContent([]) })
                : Task.FromException<HttpResponseMessage>(_fault);
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
