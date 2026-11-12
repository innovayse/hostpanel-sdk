using Hostpanel.Sdk.Plugins.Endpoints;
using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Tests.Endpoints;

/// <summary>Tests of <see cref="PluginReply.Redirect"/>.</summary>
public sealed class PluginReplyRedirectTests
{
    /// <summary>A redirect keeps its location, answers 303 by default and is of the redirect kind.</summary>
    [Fact]
    public void Redirect_DefaultStatus_IsSeeOther()
    {
        var reply = PluginReply.Redirect("/invoices/7");

        reply.Kind.Should().Be(PluginReplyKind.Redirect);
        reply.Status.Should().Be(303);
        reply.Location.Should().Be("/invoices/7");
        reply.Body.Should().BeNull();
        reply.RawBody.IsEmpty.Should().BeTrue();
    }

    /// <summary>Every redirect status the contract names is accepted.</summary>
    /// <param name="status">The status to build the reply with.</param>
    [Theory]
    [InlineData(301)]
    [InlineData(302)]
    [InlineData(303)]
    [InlineData(307)]
    [InlineData(308)]
    public void Redirect_RedirectStatus_IsAccepted(int status)
    {
        PluginReply.Redirect("/done", status).Status.Should().Be(status);
    }

    /// <summary>A status that is not one of the five redirect statuses is refused.</summary>
    /// <param name="status">The status to build the reply with.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(200)]
    [InlineData(300)]
    [InlineData(304)]
    [InlineData(305)]
    [InlineData(306)]
    [InlineData(309)]
    [InlineData(404)]
    public void Redirect_OtherStatus_Throws(int status)
    {
        var build = () => PluginReply.Redirect("/done", status);

        build.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>A host relative path and an absolute https address are accepted.</summary>
    /// <param name="location">The location to redirect to.</param>
    [Theory]
    [InlineData("/")]
    [InlineData("/invoices/7?paid=1#top")]
    [InlineData("https://host.example/invoices/7")]
    [InlineData("https://host.example:8443/a?b=c")]
    public void Redirect_AcceptedLocation_Builds(string location)
    {
        PluginReply.Redirect(location).Location.Should().Be(location);
    }

    /// <summary>
    /// A protocol relative address, a backslash, a line break, a non https address, user info and a relative path
    /// without a leading slash are refused.
    /// </summary>
    /// <param name="location">The location to redirect to.</param>
    [Theory]
    [InlineData("//evil.example/path")]
    [InlineData("/\\evil.example")]
    [InlineData("\\\\evil.example")]
    [InlineData("/done\r\nSet-Cookie: a=b")]
    [InlineData("/done\nx")]
    [InlineData("http://host.example/a")]
    [InlineData("ftp://host.example/a")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:pass@host.example/a")]
    [InlineData("https://user@host.example/a")]
    [InlineData("invoices/7")]
    public void Redirect_RefusedLocation_Throws(string location)
    {
        var build = () => PluginReply.Redirect(location);

        build.Should().Throw<ArgumentException>();
    }

    /// <summary>An empty or white space location is refused.</summary>
    /// <param name="location">The location to redirect to.</param>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Redirect_BlankLocation_Throws(string location)
    {
        var build = () => PluginReply.Redirect(location);

        build.Should().Throw<ArgumentException>();
    }

    /// <summary>A null location is refused.</summary>
    [Fact]
    public void Redirect_NullLocation_Throws()
    {
        var build = () => PluginReply.Redirect(null!);

        build.Should().Throw<ArgumentNullException>();
    }

    /// <summary>The reply kinds keep their values and the redirect is appended after the raw reply.</summary>
    [Fact]
    public void PluginReplyKind_Values_ArePinned()
    {
        Enum.GetValues<PluginReplyKind>().Select(value => (int)value).Should().Equal(1, 2, 3, 4, 5, 6);
        PluginReplyKind.Redirect.Should().Be((PluginReplyKind)6);
    }

    /// <summary>The other replies carry no location.</summary>
    [Fact]
    public void OtherReplies_HaveNoLocation()
    {
        PluginReply.NoContent().Location.Should().BeNull();
        PluginReply.Json(new { a = 1 }).Location.Should().BeNull();
    }
}
