using Hostpanel.Sdk.Packaging.Naming;

namespace Hostpanel.Sdk.Packaging.Tests.Naming;

/// <summary>Tests of <see cref="PluginIdentifier"/> and <see cref="PluginPageIdentifier"/>.</summary>
public sealed class PluginIdentifierTests
{
    /// <summary>A plugin id is lower-case words joined by single hyphens, with nothing a file system or URL could read as a separator.</summary>
    /// <param name="id">The candidate.</param>
    /// <param name="expected">Whether it is well formed.</param>
    [Theory]
    [InlineData("demo", true)]
    [InlineData("demo-pay", true)]
    [InlineData("a1-b2-c3", true)]
    [InlineData("", false)]
    [InlineData("-demo", false)]
    [InlineData("demo-", false)]
    [InlineData("de--mo", false)]
    [InlineData("Demo", false)]
    [InlineData("de_mo", false)]
    [InlineData("de.mo", false)]
    [InlineData("../demo", false)]
    [InlineData("de/mo", false)]
    [InlineData("de\\mo", false)]
    public void IsWellFormed_FollowsTheShape(string id, bool expected)
    {
        PluginIdentifier.IsWellFormed(id).Should().Be(expected);
    }

    /// <summary>A trailing line feed is not part of an id: the end anchor is <c>\z</c>, not <c>$</c>.</summary>
    [Fact]
    public void IsWellFormed_TrailingNewline_IsRefused()
    {
        PluginIdentifier.IsWellFormed("demo\n").Should().BeFalse();
    }

    /// <summary>An id is at most 64 characters, and a missing one is not an id.</summary>
    [Fact]
    public void IsWellFormed_BoundsTheLength()
    {
        PluginIdentifier.IsWellFormed(new string('a', 64)).Should().BeTrue();
        PluginIdentifier.IsWellFormed(new string('a', 65)).Should().BeFalse();
        PluginIdentifier.IsWellFormed(null).Should().BeFalse();
    }

    /// <summary>The ids the host keeps for its own routes are well formed but cannot be taken by a new plugin.</summary>
    /// <param name="id">The reserved id.</param>
    [Theory]
    [InlineData("ui")]
    [InlineData("slots")]
    public void IsValid_ReservedId_IsRefusedButWellFormed(string id)
    {
        PluginIdentifier.IsWellFormed(id).Should().BeTrue();
        PluginIdentifier.IsReserved(id).Should().BeTrue();
        PluginIdentifier.IsValid(id).Should().BeFalse();
    }

    /// <summary>A page id starts with a letter or digit and holds lower-case letters, digits, hyphens and underscores.</summary>
    /// <param name="id">The candidate.</param>
    /// <param name="expected">Whether it is valid.</param>
    [Theory]
    [InlineData("dashboard", true)]
    [InlineData("a_b-c", true)]
    [InlineData("0page", true)]
    [InlineData("by-path", false)]
    [InlineData("_page", false)]
    [InlineData("Page", false)]
    [InlineData("pa ge", false)]
    [InlineData("page\n", false)]
    [InlineData("", false)]
    public void PageIdentifier_IsValid_FollowsTheShape(string id, bool expected)
    {
        PluginPageIdentifier.IsValid(id).Should().Be(expected);
    }

    /// <summary>A page id is at most 64 characters.</summary>
    [Fact]
    public void PageIdentifier_IsValid_BoundsTheLength()
    {
        PluginPageIdentifier.IsValid(new string('a', 64)).Should().BeTrue();
        PluginPageIdentifier.IsValid(new string('a', 65)).Should().BeFalse();
    }
}
