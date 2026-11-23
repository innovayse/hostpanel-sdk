using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Tests.Support;

namespace Hostpanel.Sdk.Tests.Manifest;

/// <summary>Tests of <see cref="PluginSshAgentLayout"/> and the <see cref="PluginManifest.SshAgentProfile"/> member.</summary>
public sealed class PluginSshAgentLayoutTests
{
    /// <summary>The paths, names and limits are pinned: a change would break every package and host that relies on them.</summary>
    [Fact]
    public void Constants_ArePinned()
    {
        PluginSshAgentLayout.ProfileFile.Should().Be("agent/profile.conf");
        PluginSshAgentLayout.HelperDirectory.Should().Be("agent/helpers");
        PluginSshAgentLayout.ReservedProfile.Should().Be("generic-linux");
        PluginSshAgentLayout.MaxProfileBytes.Should().Be(8192);
        PluginSshAgentLayout.MaxHelperCount.Should().Be(32);
        PluginSshAgentLayout.MaxHelperBytes.Should().Be(65536);
        PluginSshAgentLayout.MaxProfileIdLength.Should().Be(32);
        PluginSshAgentLayout.MaxHelperNameLength.Should().Be(64);
    }

    /// <summary>Lower case words and digits joined by single hyphens are profile ids, up to the length cap.</summary>
    [Theory]
    [InlineData("a")]
    [InlineData("0")]
    [InlineData("demo")]
    [InlineData("demo-panel")]
    [InlineData("demo-panel-2")]
    [InlineData("generic-linux")]
    public void IsValidProfileId_WellFormedId_IsTrue(string id)
    {
        PluginSshAgentLayout.IsValidProfileId(id).Should().BeTrue();
    }

    /// <summary>The longest id is accepted and one more character is not.</summary>
    [Fact]
    public void IsValidProfileId_LengthCap_IsExact()
    {
        PluginSshAgentLayout.IsValidProfileId(new string('a', PluginSshAgentLayout.MaxProfileIdLength)).Should().BeTrue();
        PluginSshAgentLayout.IsValidProfileId(new string('a', PluginSshAgentLayout.MaxProfileIdLength + 1)).Should().BeFalse();
    }

    /// <summary>A text that is missing, empty, in upper case, hyphenated badly or holding another character is not a profile id.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Demo")]
    [InlineData("-demo")]
    [InlineData("demo-")]
    [InlineData("demo--panel")]
    [InlineData("demo_panel")]
    [InlineData("demo.panel")]
    [InlineData("demo panel")]
    [InlineData("demo/panel")]
    [InlineData("demo\n")]
    [InlineData("d\u00e9mo")]
    public void IsValidProfileId_OtherText_IsFalse(string? id)
    {
        PluginSshAgentLayout.IsValidProfileId(id).Should().BeFalse();
    }

    /// <summary>A name that starts with a letter or digit and holds letters, digits, dots, underscores and hyphens is a helper name.</summary>
    [Theory]
    [InlineData("a")]
    [InlineData("account-check")]
    [InlineData("ensure_website.sh")]
    [InlineData("1.helper")]
    [InlineData("Helper-2")]
    public void IsValidHelperName_WellFormedName_IsTrue(string name)
    {
        PluginSshAgentLayout.IsValidHelperName(name).Should().BeTrue();
    }

    /// <summary>The longest name is accepted and one more character is not.</summary>
    [Fact]
    public void IsValidHelperName_LengthCap_IsExact()
    {
        PluginSshAgentLayout.IsValidHelperName(new string('a', PluginSshAgentLayout.MaxHelperNameLength)).Should().BeTrue();
        PluginSshAgentLayout.IsValidHelperName(new string('a', PluginSshAgentLayout.MaxHelperNameLength + 1)).Should().BeFalse();
    }

    /// <summary>A name that is missing, hidden, a parent folder, holds a separator, a space or a non ASCII letter is not a helper name.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(".hidden")]
    [InlineData("..")]
    [InlineData("-helper")]
    [InlineData("_helper")]
    [InlineData("a/b")]
    [InlineData("a\b")]
    [InlineData("a b")]
    [InlineData("a\n")]
    [InlineData("h\u00e9lper")]
    public void IsValidHelperName_OtherText_IsFalse(string? name)
    {
        PluginSshAgentLayout.IsValidHelperName(name).Should().BeFalse();
    }

    /// <summary>A manifest without the member reads null, and one that sets it keeps the id.</summary>
    [Fact]
    public void Manifest_SshAgentProfile_DefaultsToNull()
    {
        PluginManifestFixtures.Minimal().SshAgentProfile.Should().BeNull();
        PluginManifestFixtures.Minimal(sshAgentProfile: "demo-panel").SshAgentProfile.Should().Be("demo-panel");
    }
}
