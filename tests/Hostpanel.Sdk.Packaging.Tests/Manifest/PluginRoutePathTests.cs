using Hostpanel.Sdk.Packaging.Manifest;
using Hostpanel.Sdk.Packaging.Naming;

namespace Hostpanel.Sdk.Packaging.Tests.Manifest;

/// <summary>Tests of <see cref="PluginRoutePath"/>.</summary>
public sealed class PluginRoutePathTests
{
    /// <summary>Valid paths normalise to a template where every parameter name is the same placeholder.</summary>
    /// <param name="path">The declared path.</param>
    /// <param name="expected">The template.</param>
    [Theory]
    [InlineData("/orders", "/orders")]
    [InlineData("/orders/{id}", "/orders/{}")]
    [InlineData("/orders/{orderId}/lines/{lineId}", "/orders/{}/lines/{}")]
    [InlineData("/a-b_c/0", "/a-b_c/0")]
    [InlineData("/a", "/a")]
    [InlineData("/{id}", "/{}")]
    [InlineData("/0/9", "/0/9")]
    [InlineData("/a/{x1}/b", "/a/{}/b")]
    [InlineData("/a/{id}/{id}", "/a/{}/{}")]
    public void TryNormalize_ValidPath_ReturnsTheTemplate(string path, string expected)
    {
        PluginRoutePath.TryNormalize(path, out var template).Should().BeTrue();
        template.Should().Be(expected);
    }

    /// <summary>Paths that are not a slash plus lowercase literals and parameters are refused.</summary>
    /// <param name="path">The declared path.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("orders")]
    [InlineData("/orders/")]
    [InlineData("//orders")]
    [InlineData("/orders//lines")]
    [InlineData("/Orders")]
    [InlineData("/orders/..")]
    [InlineData("/orders/../x")]
    [InlineData("/orders/.")]
    [InlineData("/orders/{id")]
    [InlineData("/orders/{1id}")]
    [InlineData("/orders/a{id}")]
    [InlineData("/orders\n")]
    [InlineData("/orders/{id}\n")]
    public void TryNormalize_InvalidPath_IsRefused(string? path)
    {
        PluginRoutePath.TryNormalize(path, out _).Should().BeFalse();
    }

    /// <summary>More refused shapes: a hidden segment, a segment that starts with a separator character, parameter spelling, white space and non-ASCII letters.</summary>
    /// <param name="path">The declared path.</param>
    [Theory]
    [InlineData("/.hidden")]
    [InlineData("/-a")]
    [InlineData("/_a")]
    [InlineData("/a b")]
    [InlineData("/ a")]
    [InlineData("/a\t")]
    [InlineData("/a?x=1")]
    [InlineData("/a#top")]
    [InlineData("/a:b")]
    [InlineData("/a%2e")]
    [InlineData("/%2e%2e/x")]
    [InlineData("/a/{}")]
    [InlineData("/a/{ id }")]
    [InlineData("/a/{id-x}")]
    [InlineData("/a/{id_x}")]
    [InlineData("/a/{{id}}")]
    [InlineData("/a/}id{")]
    [InlineData("/a/{id}x")]
    [InlineData("/a/*")]
    [InlineData("/é")]
    [InlineData("/а")]
    [InlineData("/a\\b")]
    [InlineData("\\a")]
    [InlineData(" /a")]
    [InlineData("/a/\u0000")]
    public void TryNormalize_MoreInvalidShapes_AreRefused(string path)
    {
        PluginRoutePath.TryNormalize(path, out _).Should().BeFalse();
    }

    /// <summary>
    /// A literal route segment starts with a lowercase letter or digit and holds lowercase letters, digits, hyphens and
    /// underscores; anything else, an empty segment and a trailing line feed included, is refused.
    /// </summary>
    /// <param name="segment">The candidate.</param>
    /// <param name="accepted">Whether a path with this segment is accepted.</param>
    [Theory]
    [InlineData("orders", true)]
    [InlineData("a-b_c0", true)]
    [InlineData("0lines", true)]
    [InlineData("_orders", false)]
    [InlineData("-orders", false)]
    [InlineData("Orders", false)]
    [InlineData("or ders", false)]
    [InlineData("orders\n", false)]
    [InlineData("", false)]
    public void LiteralSegment_FollowsTheShape(string segment, bool accepted)
    {
        PluginRoutePath.TryNormalize("/" + segment, out _).Should().Be(accepted);
    }

    /// <summary>A page id has the same shape as a literal segment, and the same cases give the same answers.</summary>
    /// <param name="segment">The candidate.</param>
    /// <param name="accepted">Whether it is a valid page id.</param>
    [Theory]
    [InlineData("orders", true)]
    [InlineData("a-b_c0", true)]
    [InlineData("0lines", true)]
    [InlineData("_orders", false)]
    [InlineData("-orders", false)]
    [InlineData("Orders", false)]
    [InlineData("or ders", false)]
    [InlineData("orders\n", false)]
    [InlineData("", false)]
    public void PageId_FollowsTheSameShape(string segment, bool accepted)
    {
        PluginPageIdentifier.IsValid(segment).Should().Be(accepted);
    }

    /// <summary>A refused path leaves the template empty, so a caller that reads it anyway gets nothing.</summary>
    [Fact]
    public void TryNormalize_InvalidPath_LeavesTheTemplateEmpty()
    {
        PluginRoutePath.TryNormalize("/ok/Bad", out var template);

        template.Should().BeEmpty();
    }
}
