using System.Text;
using Hostpanel.Sdk.Packaging.Agent;
using Hostpanel.Sdk.Packaging.Enums;
using Hostpanel.Sdk.Packaging.Tests.Support;
using Hostpanel.Sdk.Plugins.Manifest;
using static Hostpanel.Sdk.Packaging.Tests.Agent.AgentFixtures;

namespace Hostpanel.Sdk.Packaging.Tests.Agent;

/// <summary>Tests of <see cref="PluginAgentPackageChecker"/> on the profile file: presence, size, links, format, keys and values.</summary>
public sealed class PluginAgentProfileFileTests
{
    /// <summary>Asserts that a profile breaks exactly one rule.</summary>
    /// <param name="profile">The profile content.</param>
    /// <param name="rule">The rule expected.</param>
    private static void AssertBreaks(string profile, PluginAgentRule rule)
    {
        var result = CheckProfile(profile);

        result.Files.Should().BeNull();
        result.Violation!.Rule.Should().Be(rule);
    }

    /// <summary>A manifest that names no profile has nothing to check, even in a package that carries agent files.</summary>
    [Fact]
    public void Check_NoProfileNamed_IsNotAnError()
    {
        using var folder = new TempFolder();
        folder.Write(ProfilePath, "not even a profile");

        var result = PluginAgentPackageChecker.Check(folder.Path, null);

        result.Files.Should().BeNull();
        result.Violation.Should().BeNull();
    }

    /// <summary>A good profile is accepted and its bytes are returned with the profile id.</summary>
    [Fact]
    public void Check_GoodProfile_IsAccepted()
    {
        var result = CheckProfile(GoodProfile);

        result.Violation.Should().BeNull();
        result.Files!.ProfileId.Should().Be(ProfileId);
        result.Files.Profile.Should().Equal(Encoding.UTF8.GetBytes(GoodProfile));
        result.Files.Helpers.Should().BeEmpty();
    }

    /// <summary>A package folder given with a trailing separator is the same folder.</summary>
    [Fact]
    public void Check_PackageFolderWithTrailingSeparator_IsAccepted()
    {
        using var folder = new TempFolder();
        folder.Write(ProfilePath, GoodProfile);

        var result = PluginAgentPackageChecker.Check(folder.Path + Path.DirectorySeparatorChar, ProfileId);

        result.Violation.Should().BeNull();
        result.Files.Should().NotBeNull();
    }

    /// <summary>Windows line endings, comments with either marker and blank lines are read as the agent reads them.</summary>
    [Fact]
    public void Check_CrLfCommentsAndBlankLines_AreAccepted()
    {
        CheckProfile("; one\r\n\r\n[agent]\r\n# two\r\nprovider = demo-panel\r\n\r\n").Violation.Should().BeNull();
    }

    /// <summary>A package without a profile file is refused as missing, and the detail holds no host path.</summary>
    [Fact]
    public void Check_NoProfileFile_IsMissing()
    {
        using var folder = new TempFolder();

        var result = PluginAgentPackageChecker.Check(folder.Path, ProfileId);

        result.Violation!.Rule.Should().Be(PluginAgentRule.ProfileMissing);
        result.Violation.Detail.Should().Contain(ProfilePath).And.NotContain(folder.Path);
    }

    /// <summary>A profile at the size cap is accepted and one byte more is refused.</summary>
    [Fact]
    public void Check_ProfileSizeCap_IsExact()
    {
        var atCap = GoodProfile + "#" + new string('x', PluginSshAgentLayout.MaxProfileBytes - GoodProfile.Length - 2) + "\n";
        atCap.Length.Should().Be(PluginSshAgentLayout.MaxProfileBytes);

        CheckProfile(atCap).Violation.Should().BeNull();
        AssertBreaks(atCap + "x", PluginAgentRule.ProfileInvalid);
    }

    /// <summary>A profile path that is a folder is not a profile file: it does not exist as one.</summary>
    [Fact]
    public void Check_ProfileThatIsAFolder_IsMissing()
    {
        using var folder = new TempFolder();
        Directory.CreateDirectory(Path.Combine(folder.Path, "agent", "profile.conf"));

        PluginAgentPackageChecker.Check(folder.Path, ProfileId).Violation!.Rule.Should().Be(PluginAgentRule.ProfileMissing);
    }

    /// <summary>A profile that is a symbolic link is refused, whatever it points at.</summary>
    [Fact]
    public void Check_ProfileThatIsALink_IsRefused()
    {
        using var folder = new TempFolder();
        using var outside = new TempFolder();
        var target = outside.Write("real.conf", GoodProfile);
        Directory.CreateDirectory(Path.Combine(folder.Path, "agent"));
        Assert.True(
            SymbolicLinks.TryCreateFileLink(Path.Combine(folder.Path, "agent", "profile.conf"), target),
            "This account may not create symbolic links, so there is no link to refuse.");

        var result = PluginAgentPackageChecker.Check(folder.Path, ProfileId);

        result.Violation!.Rule.Should().Be(PluginAgentRule.LinkNotAllowed);
        result.Violation.Detail.Should().NotContain(folder.Path).And.NotContain(outside.Path);
    }

    /// <summary>A profile reached through a folder that is a symbolic link is refused.</summary>
    [Fact]
    public void Check_ProfileBehindALinkedFolder_IsRefused()
    {
        using var folder = new TempFolder();
        using var outside = new TempFolder();
        outside.Write("profile.conf", GoodProfile);
        Assert.True(
            SymbolicLinks.TryCreateFolderLink(Path.Combine(folder.Path, "agent"), outside.Path),
            "This account may not create symbolic links, so there is no link to refuse.");

        PluginAgentPackageChecker.Check(folder.Path, ProfileId).Violation!.Rule.Should().Be(PluginAgentRule.LinkNotAllowed);
    }

    /// <summary>A text that is not UTF-8, holds a control character or starts with a byte order mark is refused.</summary>
    [Fact]
    public void Check_ProfileThatIsNotPlainText_IsInvalid()
    {
        CheckProfile([0xFF, 0xFE, 0x00, 0x5B]).Violation!.Rule.Should().Be(PluginAgentRule.ProfileInvalid);
        AssertBreaks(GoodProfile + "max_keys = \u0007\n", PluginAgentRule.ProfileInvalid);
        AssertBreaks("\uFEFF" + GoodProfile, PluginAgentRule.ProfileInvalid);
    }

    /// <summary>A profile the agent's reader would read differently than a person does is refused.</summary>
    /// <param name="profile">The profile content.</param>
    [Theory]
    [InlineData("")]
    [InlineData("# only a comment\n")]
    [InlineData("provider = demo-panel\n[agent]\n")]
    [InlineData("[other]\nprovider = demo-panel\n")]
    [InlineData("[agent]\nprovider = demo-panel\n[agent]\n")]
    [InlineData("[agent]\nprovider = demo-panel\n[extra]\nmax_keys = 1\n")]
    [InlineData("[AGENT]\nprovider = demo-panel\n")]
    [InlineData("[agent]\nprovider = demo-panel\n  max_keys = 1\n")]
    [InlineData("[agent]\nprovider = demo-panel\n\tmax_keys = 1\n")]
    [InlineData("[agent]\nprovider: demo-panel\n")]
    [InlineData("[agent]\nprovider = demo-panel\nmax_keys\n")]
    [InlineData("[agent]\nprovider = demo-panel\nMax_Keys = 1\n")]
    [InlineData("[agent]\nprovider = demo-panel\n = 1\n")]
    [InlineData("[agent]\nprovider = demo-panel\nmax-keys = 1\n")]
    [InlineData("[agent]\nprovider = demo-panel\nmax_keys = 1\nmax_keys = 2\n")]
    [InlineData("[agent]\nprovider = demo-panel\njail_shell = /bin/%(x)s\n")]
    public void Check_UnreadableProfile_IsInvalid(string profile)
    {
        AssertBreaks(profile, PluginAgentRule.ProfileInvalid);
    }

    /// <summary>A key the agent does not define is refused, and the detail names the key and no value.</summary>
    [Fact]
    public void Check_UnknownKey_IsNotAllowed()
    {
        var result = CheckProfile(WithLine("run_command = /bin/sh -c secret-value"));

        result.Violation!.Rule.Should().Be(PluginAgentRule.KeyNotAllowed);
        result.Violation.Detail.Should().Contain("run_command").And.NotContain("secret-value");
    }

    /// <summary>Every key the agent defines is accepted.</summary>
    [Fact]
    public void Check_EveryAgentKey_IsAccepted()
    {
        var keys = new[]
        {
            "min_uid = 1000", "home_root = /home", "deny_accounts = root", "max_keys = 20", "audit_log = /var/log/a.log",
            "lock_dir = /run/a", "ssh_group =", "eligible_list =", "check_shadow_lock = true", "jail_enrol = false",
            "jail_root = /home/jail", "jail_group = sftp-ssh", "jail_sections = basicshell", "jail_shell = /bin/bash",
            "jail_extras = /usr/bin/id", "systemd_unit_dir = /etc/systemd/system",
        };

        CheckProfile(Section(["provider = " + ProfileId, .. keys])).Violation.Should().BeNull();
    }

    /// <summary>A provider that is missing or is not the manifest's id is refused, letter case included.</summary>
    /// <param name="providerLine">The line that sets the provider, or an empty text for none.</param>
    [Theory]
    [InlineData("")]
    [InlineData("provider = other-panel")]
    [InlineData("provider = Demo-Panel")]
    [InlineData("provider = generic-linux")]
    [InlineData("provider =")]
    public void Check_ProviderThatIsNotTheId_IsMismatch(string providerLine)
    {
        AssertBreaks(Section(providerLine, "max_keys = 20"), PluginAgentRule.ProviderMismatch);
    }

    /// <summary>A binding that is set is refused; an empty or absent one is accepted.</summary>
    [Fact]
    public void Check_Binding_MustBeEmpty()
    {
        AssertBreaks(WithLine("binding = 3f2b8c1e-0000-4000-8000-000000000001"), PluginAgentRule.BindingNotEmpty);
        CheckProfile(WithLine("binding =")).Violation.Should().BeNull();
        CheckProfile(Section("provider = " + ProfileId)).Violation.Should().BeNull();
    }

    /// <summary>A path that leaves the helper folder of the agent, by any route, is refused for both keys.</summary>
    /// <param name="key">The key that names the path.</param>
    /// <param name="value">The path.</param>
    [Theory]
    [InlineData("account_check_command", "/usr/bin/id")]
    [InlineData("account_check_command", "account-check")]
    [InlineData("account_check_command", "/usr/local/libexec/hostpanel-ssh-key-agent/../../../bin/sh")]
    [InlineData("account_check_command", "/usr/local/libexec/hostpanel-ssh-key-agent/./check")]
    [InlineData("account_check_command", "/usr/local/libexec/hostpanel-ssh-key-agent//check")]
    [InlineData("account_check_command", "/usr/local/libexec/hostpanel-ssh-key-agent/")]
    [InlineData("account_check_command", "/usr/local/libexec/hostpanel-ssh-key-agent-other/check")]
    [InlineData("account_check_command", "/usr/local/libexec/hostpanel-ssh-key-agent/check --flag")]
    [InlineData("account_check_command", "/usr/local/libexec/hostpanel-ssh-key-agent/check;id")]
    [InlineData("helper_dir", "/tmp")]
    [InlineData("helper_dir", "/usr/local/libexec")]
    [InlineData("helper_dir", "/usr/local/libexec/hostpanel-ssh-key-agent/..")]
    [InlineData("helper_dir", "/usr/local/libexec/hostpanel-ssh-key-agent/sub/../../x")]
    public void Check_PathOutsideTheHelperFolder_IsRefused(string key, string value)
    {
        var result = CheckProfile(WithLine($"{key} = {value}"));

        result.Violation!.Rule.Should().Be(PluginAgentRule.PathOutsideHelperDirectory);
        result.Violation.Detail.Should().Contain(key);
    }

    /// <summary>An empty value, the helper folder itself and something below it are accepted for both keys.</summary>
    /// <param name="line">The line that sets the path.</param>
    [Theory]
    [InlineData("helper_dir =")]
    [InlineData("helper_dir = /usr/local/libexec/hostpanel-ssh-key-agent")]
    [InlineData("helper_dir = /usr/local/libexec/hostpanel-ssh-key-agent/demo")]
    [InlineData("account_check_command =")]
    [InlineData("account_check_command = /usr/local/libexec/hostpanel-ssh-key-agent/account-check.sh")]
    public void Check_PathInsideTheHelperFolder_IsAccepted(string line)
    {
        CheckProfile(WithLine(line)).Violation.Should().BeNull();
    }
}
