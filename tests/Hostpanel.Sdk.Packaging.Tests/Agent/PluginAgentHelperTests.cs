using System.Text;
using Hostpanel.Sdk.Packaging.Agent;
using Hostpanel.Sdk.Packaging.Enums;
using Hostpanel.Sdk.Packaging.Tests.Support;
using Hostpanel.Sdk.Plugins.Manifest;
using static Hostpanel.Sdk.Packaging.Tests.Agent.AgentFixtures;

namespace Hostpanel.Sdk.Packaging.Tests.Agent;

/// <summary>Tests of <see cref="PluginAgentPackageChecker"/> on the helpers: names, count, size, folders and links.</summary>
public sealed class PluginAgentHelperTests
{
    /// <summary>Makes a package folder that holds a good profile.</summary>
    /// <returns>The folder, deleted when disposed.</returns>
    private static TempFolder Package()
    {
        var folder = new TempFolder();
        folder.Write(ProfilePath, GoodProfile);
        return folder;
    }

    /// <summary>Asserts that a package breaks exactly one rule.</summary>
    /// <param name="folder">The package folder.</param>
    /// <param name="rule">The rule expected.</param>
    /// <returns>The violation, for more assertions.</returns>
    private static PluginAgentViolation AssertBreaks(TempFolder folder, PluginAgentRule rule)
    {
        var result = PluginAgentPackageChecker.Check(folder.Path, ProfileId);

        result.Files.Should().BeNull();
        result.Violation!.Rule.Should().Be(rule);
        result.Violation.Detail.Should().NotContain(folder.Path);
        return result.Violation;
    }

    /// <summary>Helpers are returned by name, in ordinal order, with the bytes that were checked.</summary>
    [Fact]
    public void Check_Helpers_AreReturnedByName()
    {
        using var folder = Package();
        folder.Write("agent/helpers/b-helper", "two");
        folder.Write("agent/helpers/account-check", "one");
        folder.Write("agent/helpers/A.sh", "three");

        var result = PluginAgentPackageChecker.Check(folder.Path, ProfileId);

        result.Violation.Should().BeNull();
        result.Files!.Helpers.Keys.Should().Equal("A.sh", "account-check", "b-helper");
        result.Files.Helpers["account-check"].Should().Equal(Encoding.UTF8.GetBytes("one"));
    }

    /// <summary>An empty helper folder is the same as none.</summary>
    [Fact]
    public void Check_EmptyHelperFolder_IsAccepted()
    {
        using var folder = Package();
        Directory.CreateDirectory(Path.Combine(folder.Path, "agent", "helpers"));

        PluginAgentPackageChecker.Check(folder.Path, ProfileId).Files!.Helpers.Should().BeEmpty();
    }

    /// <summary>A helper at the size cap is accepted and one byte more is refused.</summary>
    [Fact]
    public void Check_HelperSizeCap_IsExact()
    {
        using var folder = Package();
        folder.Write("agent/helpers/tool", new byte[PluginSshAgentLayout.MaxHelperBytes]);

        PluginAgentPackageChecker.Check(folder.Path, ProfileId).Violation.Should().BeNull();

        folder.Write("agent/helpers/tool", new byte[PluginSshAgentLayout.MaxHelperBytes + 1]);

        AssertBreaks(folder, PluginAgentRule.HelperInvalid).Detail.Should().Contain("agent/helpers/tool");
    }

    /// <summary>The largest number of helpers is accepted and one more is refused before any is read.</summary>
    [Fact]
    public void Check_HelperCountCap_IsExact()
    {
        using var folder = Package();
        foreach (var number in Enumerable.Range(1, PluginSshAgentLayout.MaxHelperCount))
        {
            folder.Write($"agent/helpers/tool{number}", "x");
        }

        PluginAgentPackageChecker.Check(folder.Path, ProfileId).Files!.Helpers.Should().HaveCount(PluginSshAgentLayout.MaxHelperCount);

        folder.Write("agent/helpers/one-too-many", "x");

        AssertBreaks(folder, PluginAgentRule.TooManyHelpers);
    }

    /// <summary>A helper name that is hidden or holds a space or a non ASCII letter is refused, and the name is not echoed.</summary>
    /// <param name="name">The file name.</param>
    [Theory]
    [InlineData(".hidden")]
    [InlineData("-dash-first")]
    [InlineData("with space")]
    [InlineData("h\u00e9lper")]
    public void Check_BadHelperName_IsRefused(string name)
    {
        using var folder = Package();
        folder.Write("agent/helpers/" + name, "x");

        AssertBreaks(folder, PluginAgentRule.HelperInvalid).Detail.Should().NotContain(name);
    }

    /// <summary>A helper name over the length cap is refused.</summary>
    [Fact]
    public void Check_HelperNameOverTheCap_IsRefused()
    {
        using var folder = Package();
        folder.Write("agent/helpers/" + new string('a', PluginSshAgentLayout.MaxHelperNameLength + 1), "x");

        AssertBreaks(folder, PluginAgentRule.HelperInvalid);
    }

    /// <summary>A folder inside the helper folder is not a helper and is refused, so a helper cannot hide a tree.</summary>
    [Fact]
    public void Check_FolderInsideTheHelperFolder_IsRefused()
    {
        using var folder = Package();
        folder.Write("agent/helpers/sub/tool", "x");

        AssertBreaks(folder, PluginAgentRule.HelperInvalid);
    }

    /// <summary>A helper path that is a file instead of a folder is refused.</summary>
    [Fact]
    public void Check_HelperFolderThatIsAFile_IsRefused()
    {
        using var folder = Package();
        folder.Write("agent/helpers", "x");

        AssertBreaks(folder, PluginAgentRule.HelperInvalid);
    }

    /// <summary>A helper that is a symbolic link is refused, whatever it points at.</summary>
    [Fact]
    public void Check_HelperThatIsALink_IsRefused()
    {
        using var folder = Package();
        using var outside = new TempFolder();
        var target = outside.Write("real", "x");
        Directory.CreateDirectory(Path.Combine(folder.Path, "agent", "helpers"));
        Assert.True(
            SymbolicLinks.TryCreateFileLink(Path.Combine(folder.Path, "agent", "helpers", "tool"), target),
            "This account may not create symbolic links, so there is no link to refuse.");

        AssertBreaks(folder, PluginAgentRule.LinkNotAllowed).Detail.Should().NotContain(outside.Path);
    }

    /// <summary>A helper folder that is a symbolic link is refused and nothing behind it is listed.</summary>
    [Fact]
    public void Check_HelperFolderThatIsALink_IsRefused()
    {
        using var folder = Package();
        using var outside = new TempFolder();
        outside.Write("tool", "x");
        Assert.True(
            SymbolicLinks.TryCreateFolderLink(Path.Combine(folder.Path, "agent", "helpers"), outside.Path),
            "This account may not create symbolic links, so there is no link to refuse.");

        AssertBreaks(folder, PluginAgentRule.LinkNotAllowed).Detail.Should().NotContain(outside.Path);
    }

    /// <summary>A bad profile is reported before the helpers are looked at.</summary>
    [Fact]
    public void Check_BadProfileAndBadHelper_ReportsTheProfileFirst()
    {
        using var folder = new TempFolder();
        folder.Write(ProfilePath, "[agent]\nprovider = other\n");
        folder.Write("agent/helpers/.hidden", "x");

        AssertBreaks(folder, PluginAgentRule.ProviderMismatch);
    }
}
