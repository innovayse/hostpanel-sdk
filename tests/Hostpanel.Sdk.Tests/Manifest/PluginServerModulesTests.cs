using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Tests.Support;

namespace Hostpanel.Sdk.Tests.Manifest;

/// <summary>Tests of <see cref="PluginServerModules"/> and the <see cref="PluginManifest.ServerModules"/> member.</summary>
public sealed class PluginServerModulesTests
{
    /// <summary>A list of well formed, distinct ids has no violation.</summary>
    [Fact]
    public void Validate_GoodList_HasNoViolation()
    {
        PluginServerModules.Validate(["panel-a", "Panel.B_2", "x"]).Should().BeEmpty();
    }

    /// <summary>A missing or empty list is refused as a whole.</summary>
    [Fact]
    public void Validate_NullOrEmpty_ListEmpty()
    {
        PluginServerModules.Validate(null).Should().Equal(new PluginServerModuleViolation(PluginServerModuleRule.ListEmpty, null));
        PluginServerModules.Validate([]).Should().Equal(new PluginServerModuleViolation(PluginServerModuleRule.ListEmpty, null));
    }

    /// <summary>An empty or white space id is refused, with its position.</summary>
    [Fact]
    public void Validate_EmptyId_IdEmpty()
    {
        PluginServerModules.Validate(["ok", "", "  "]).Should().Equal(
            new PluginServerModuleViolation(PluginServerModuleRule.IdEmpty, 1),
            new PluginServerModuleViolation(PluginServerModuleRule.IdEmpty, 2));
    }

    /// <summary>An id of 50 characters is accepted and one of 51 is refused.</summary>
    [Fact]
    public void Validate_Length_BoundaryIs50()
    {
        PluginServerModules.Validate([new string('a', 50)]).Should().BeEmpty();
        PluginServerModules.Validate([new string('a', 51)]).Should().Equal(new PluginServerModuleViolation(PluginServerModuleRule.IdTooLong, 0));
    }

    /// <summary>Characters outside letters, digits, dash, dot and underscore are refused, including non ASCII letters.</summary>
    [Theory]
    [InlineData("a b")]
    [InlineData("a/b")]
    [InlineData("a:b")]
    [InlineData("é")]
    [InlineData("a\nb")]
    public void Validate_BadCharacters_IdBadCharacters(string id)
    {
        PluginServerModules.Validate([id]).Should().Equal(new PluginServerModuleViolation(PluginServerModuleRule.IdBadCharacters, 0));
        PluginServerModules.IsWellFormed(id).Should().BeFalse();
    }

    /// <summary>A repeated id is refused ignoring case, on the later position only.</summary>
    [Fact]
    public void Validate_Duplicate_IdDuplicate()
    {
        PluginServerModules.Validate(["Panel", "other", "panel"]).Should().Equal(new PluginServerModuleViolation(PluginServerModuleRule.IdDuplicate, 2));
    }

    /// <summary>A single id check answers for well formed and malformed ids.</summary>
    [Fact]
    public void IsWellFormed_AnswersPerId()
    {
        PluginServerModules.IsWellFormed("panel-1.x_y").Should().BeTrue();
        PluginServerModules.IsWellFormed(null).Should().BeFalse();
        PluginServerModules.IsWellFormed(string.Empty).Should().BeFalse();
    }

    /// <summary>A manifest without the member reads as an empty list, and one that sets it null does too.</summary>
    [Fact]
    public void Manifest_ServerModules_DefaultsToEmpty()
    {
        var plain = PluginManifestFixtures.Minimal(serverModules: null);
        var listed = PluginManifestFixtures.Minimal(serverModules: ["panel"]);

        plain.ServerModules.Should().BeEmpty();
        listed.ServerModules.Should().Equal("panel");
    }
}
