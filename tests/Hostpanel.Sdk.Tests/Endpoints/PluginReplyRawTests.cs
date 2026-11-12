using System.Text;
using Hostpanel.Sdk.Plugins.Endpoints;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Tests.Endpoints;

/// <summary>Tests of <see cref="PluginReply.Raw"/>.</summary>
public sealed class PluginReplyRawTests
{
    /// <summary>A raw reply keeps its status, bytes and media type and is of the raw kind.</summary>
    [Fact]
    public void Raw_ValidArguments_KeepsStatusBodyAndContentType()
    {
        var bytes = Encoding.UTF8.GetBytes("OK");

        var reply = PluginReply.Raw(200, bytes, "text/plain; charset=utf-8");

        reply.Kind.Should().Be(PluginReplyKind.Raw);
        reply.Status.Should().Be(200);
        reply.RawBody.ToArray().Should().Equal(bytes);
        reply.ContentType.Should().Be("text/plain; charset=utf-8");
        reply.Body.Should().BeNull();
    }

    /// <summary>An empty body is allowed, for an acknowledgement that has no text.</summary>
    [Fact]
    public void Raw_EmptyBody_IsAllowed()
    {
        var reply = PluginReply.Raw(202, ReadOnlyMemory<byte>.Empty, "text/plain");

        reply.RawBody.IsEmpty.Should().BeTrue();
    }

    /// <summary>The edges of the allowed range, and failure statuses, are accepted.</summary>
    /// <param name="status">The status to build the reply with.</param>
    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(403)]
    [InlineData(500)]
    [InlineData(599)]
    public void Raw_StatusInRange_IsAccepted(int status)
    {
        PluginReply.Raw(status, ReadOnlyMemory<byte>.Empty, "text/plain").Status.Should().Be(status);
    }

    /// <summary>A status outside 200 to 599, and the two authentication challenges, are refused.</summary>
    /// <param name="status">The status to build the reply with.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(199)]
    [InlineData(401)]
    [InlineData(407)]
    [InlineData(600)]
    public void Raw_StatusOutOfRange_Throws(int status)
    {
        var build = () => PluginReply.Raw(status, ReadOnlyMemory<byte>.Empty, "text/plain");

        build.Should().ThrowExactly<ArgumentOutOfRangeException>()
            .WithParameterName("status")
            .Where(ex => ex.Message.Contains("A raw reply needs a status from 200 to 599 other than 401 and 407; answer 403 to refuse a caller."));
    }

    /// <summary>A missing media type is refused.</summary>
    [Fact]
    public void Raw_NullContentType_Throws()
    {
        var build = () => PluginReply.Raw(200, ReadOnlyMemory<byte>.Empty, null!);

        build.Should().Throw<ArgumentNullException>();
    }

    /// <summary>A blank media type, or one with a line break, is refused.</summary>
    /// <param name="contentType">The media type to build the reply with.</param>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("text/plain\r\nX-Injected: 1")]
    [InlineData("text/plain\n")]
    public void Raw_BlankOrMultilineContentType_Throws(string contentType)
    {
        var build = () => PluginReply.Raw(200, ReadOnlyMemory<byte>.Empty, contentType);

        build.Should().Throw<ArgumentException>();
    }

    /// <summary>The other reply kinds carry no raw body or media type.</summary>
    [Fact]
    public void OtherKinds_HaveNoRawBody()
    {
        PluginReply.NoContent().RawBody.IsEmpty.Should().BeTrue();
        PluginReply.NoContent().ContentType.Should().BeNull();
    }
}
