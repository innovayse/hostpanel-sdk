using Hostpanel.Sdk.Packaging.Agent;
using Hostpanel.Sdk.Packaging.Tests.Support;

namespace Hostpanel.Sdk.Packaging.Tests.Agent;

/// <summary>The profiles and helpers the agent checker tests share, so a test class holds only tests.</summary>
public static class AgentFixtures
{
    /// <summary>The profile id the good package names.</summary>
    public const string ProfileId = "demo-panel";

    /// <summary>The package-relative path of the profile file.</summary>
    public const string ProfilePath = "agent/profile.conf";

    /// <summary>The standard helper folder of the agent, which a profile may point into.</summary>
    public const string HelperDirectory = "/usr/local/libexec/hostpanel-ssh-key-agent";

    /// <summary>A profile that meets every rule.</summary>
    public const string GoodProfile =
        "# A demo profile.\n[agent]\nprovider = " + ProfileId + "\nbinding =\nmax_keys = 20\n"
        + "account_check_command = " + HelperDirectory + "/account-check\nhelper_dir = " + HelperDirectory + "\n";

    /// <summary>Builds a profile of the one section with the given lines in it.</summary>
    /// <param name="lines">The lines of the section, each without a line ending.</param>
    /// <returns>The profile text.</returns>
    public static string Section(params string[] lines)
    {
        return "[agent]\n" + string.Join("\n", lines) + "\n";
    }

    /// <summary>Builds a profile for <see cref="ProfileId"/> that sets one more line.</summary>
    /// <param name="line">The line, without a line ending.</param>
    /// <returns>The profile text.</returns>
    public static string WithLine(string line)
    {
        return Section("provider = " + ProfileId, line);
    }

    /// <summary>Checks a package that holds the given profile.</summary>
    /// <param name="profile">The profile content.</param>
    /// <returns>The check result.</returns>
    public static PluginAgentCheck CheckProfile(string profile)
    {
        using var folder = new TempFolder();
        folder.Write(ProfilePath, profile);
        return PluginAgentPackageChecker.Check(folder.Path, ProfileId);
    }

    /// <summary>Checks a package that holds the given profile bytes.</summary>
    /// <param name="profile">The profile bytes.</param>
    /// <returns>The check result.</returns>
    public static PluginAgentCheck CheckProfile(byte[] profile)
    {
        using var folder = new TempFolder();
        folder.Write(ProfilePath, profile);
        return PluginAgentPackageChecker.Check(folder.Path, ProfileId);
    }
}
