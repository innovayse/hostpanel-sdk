using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Tests.Support;

#pragma warning disable CS0618 // This class tests the deprecated profile names, so it uses them on purpose.

namespace Hostpanel.Sdk.Tests.Manifest;

/// <summary>Tests of <see cref="PluginAgentProfiles"/> and the <see cref="PluginManifest.AgentProfile"/> member.</summary>
public sealed class PluginAgentProfilesTests
{
    /// <summary>The names are pinned: a rename would break every manifest that uses one.</summary>
    [Fact]
    public void All_ListsTheNamesThisVersionDefines()
    {
        PluginAgentProfiles.HostingPanelV7.Should().Be("hostingPanelV7");
        PluginAgentProfiles.All.Should().Equal("hostingPanelV7");
    }

    /// <summary>No profile name carries a vendor: each is a plain camel case word list.</summary>
    [Fact]
    public void All_NamesAreNeutralCamelCaseWords()
    {
        PluginAgentProfiles.All.Should().OnlyContain(name => name.Length > 0 && char.IsLower(name[0]) && name.All(char.IsAsciiLetterOrDigit));
    }

    /// <summary>A defined name is known, matched exactly.</summary>
    [Fact]
    public void IsKnown_DefinedName_IsTrue()
    {
        PluginAgentProfiles.All.Should().OnlyContain(name => PluginAgentProfiles.IsKnown(name));
    }

    /// <summary>A name that is missing, empty, padded, in another case or not defined is not known.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("hostingPanelV7 ")]
    [InlineData("HOSTINGPANELV7")]
    [InlineData("hostingpanelv7")]
    [InlineData("otherpanel")]
    public void IsKnown_OtherName_IsFalse(string? name)
    {
        PluginAgentProfiles.IsKnown(name).Should().BeFalse();
    }

    /// <summary>Both are deprecated in favour of the profile a plugin ships, and say so to a compiler.</summary>
    [Fact]
    public void ProfileNamesAndMember_AreMarkedObsolete()
    {
        typeof(PluginAgentProfiles).GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false).Should().ContainSingle();
        typeof(PluginManifest).GetProperty(nameof(PluginManifest.AgentProfile))!
            .GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false).Should().ContainSingle();
    }

    /// <summary>A manifest without the member reads null, and one that sets it keeps the name.</summary>
    [Fact]
    public void Manifest_AgentProfile_DefaultsToNull()
    {
        PluginManifestFixtures.Minimal().AgentProfile.Should().BeNull();
        PluginManifestFixtures.Minimal(agentProfile: PluginAgentProfiles.HostingPanelV7).AgentProfile.Should().Be("hostingPanelV7");
    }
}
