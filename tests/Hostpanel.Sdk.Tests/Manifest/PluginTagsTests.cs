using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Tests.Support;

namespace Hostpanel.Sdk.Tests.Manifest;

/// <summary>Tests of <see cref="PluginTags"/> and the <see cref="PluginManifest.Tags"/> member.</summary>
public sealed class PluginTagsTests
{
    /// <summary>Lowercase letters, digits and hyphens within the length bounds are well formed.</summary>
    /// <param name="tag">The tag.</param>
    [Theory]
    [InlineData("ab")]
    [InlineData("backup")]
    [InlineData("billing-2")]
    [InlineData("123")]
    public void IsWellFormed_GoodTag_True(string tag)
    {
        PluginTags.IsWellFormed(tag).Should().BeTrue();
    }

    /// <summary>A missing, short, upper case, hashed or otherwise unsafe tag is not well formed.</summary>
    /// <param name="tag">The tag.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("#backup")]
    [InlineData("Backup")]
    [InlineData("back up")]
    [InlineData("back_up")]
    public void IsWellFormed_BadTag_False(string? tag)
    {
        PluginTags.IsWellFormed(tag).Should().BeFalse();
    }

    /// <summary>The length bounds are inclusive: 30 characters pass and 31 do not.</summary>
    [Fact]
    public void IsWellFormed_LengthBounds_AreInclusive()
    {
        PluginTags.IsWellFormed(new string('a', PluginTags.MaxLength)).Should().BeTrue();
        PluginTags.IsWellFormed(new string('a', PluginTags.MaxLength + 1)).Should().BeFalse();
    }

    /// <summary>A manifest built without tags, or with <c>null</c>, has an empty list.</summary>
    [Fact]
    public void Manifest_Tags_DefaultsToEmpty()
    {
        PluginManifestFixtures.Minimal(tags: null).Tags.Should().BeEmpty();
        PluginManifestFixtures.Minimal(tags: ["backup"]).Tags.Should().Equal("backup");
    }
}
