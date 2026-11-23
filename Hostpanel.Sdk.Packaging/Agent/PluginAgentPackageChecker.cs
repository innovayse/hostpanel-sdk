using System.Text;
using Hostpanel.Sdk.Packaging.Enums;
using Hostpanel.Sdk.Plugins.Manifest;

namespace Hostpanel.Sdk.Packaging.Agent;

/// <summary>
/// Reads and checks the SSH key agent files a plugin package carries for the <c>sshAgentProfile</c> its manifest names,
/// once, for a startup load and an install alike. The profile (<see cref="PluginSshAgentLayout.ProfileFile"/>) must be a
/// small regular file in the agent's configuration format that sets only keys the agent defines, names the manifest's
/// profile id as its <c>provider</c>, leaves <c>binding</c> empty and points <c>account_check_command</c> and
/// <c>helper_dir</c> only into the agent's standard helper folder. The helpers
/// (<see cref="PluginSshAgentLayout.HelperDirectory"/>) are plainly named regular files within a count and size cap. No
/// file or folder on the way may be a link. The bytes are returned so a host installs exactly what was checked.
/// </summary>
/// <remarks>
/// Every refusal detail is built from fixed text, a package-relative path made of a checked name, or a profile key that
/// matched the key shape: a value taken from a file, an absolute path or an exception message never reaches a log line or
/// a response.
/// </remarks>
public static class PluginAgentPackageChecker
{
    /// <summary>The opening of the one section header.</summary>
    private const string SectionHeader = "[" + ConfigKeys.Section + "]";

    /// <summary>The separator of a package-relative path.</summary>
    private const char PackagePathSeparator = '/';

    /// <summary>The characters that separate a key from its value in the configuration format.</summary>
    private static readonly char[] Delimiters = ['=', ':'];

    /// <summary>The comment markers of a whole line in the configuration format.</summary>
    private static readonly char[] CommentMarkers = ['#', ';'];

    /// <summary>The way profile text is decoded: UTF-8 without a byte order mark that refuses a byte sequence that is not text.</summary>
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>Reads the agent files the manifest names a profile for, if any, and checks them against the agent rules.</summary>
    /// <param name="packageDir">The package folder: the installed folder at load, the unpacked staging folder at install.</param>
    /// <param name="profileId">The manifest's <c>sshAgentProfile</c>, or <see langword="null"/> when it names none. Whether the id is well formed is the manifest check's question, not this one's.</param>
    /// <returns>The checked files, nothing when the manifest names no profile, or the first rule the package breaks.</returns>
    public static PluginAgentCheck Check(string packageDir, string? profileId)
    {
        ArgumentException.ThrowIfNullOrEmpty(packageDir);

        if (profileId is null)
        {
            return new PluginAgentCheck(null, null);
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(packageDir));
        var profile = PackageFileReader.Read(root, PluginSshAgentLayout.ProfileFile, PluginSshAgentLayout.MaxProfileBytes);
        if (profile.Outcome != PackageFileOutcome.Read)
        {
            return Refused(RefusalFor(profile.Outcome, PluginSshAgentLayout.ProfileFile, isProfile: true));
        }

        var violation = CheckProfile(profile.Content!, profileId);
        if (violation is not null)
        {
            return Refused(violation);
        }

        var helpers = ReadHelpers(root, out var helperViolation);
        return helperViolation is not null
            ? Refused(helperViolation)
            : new PluginAgentCheck(new PluginAgentFiles(profileId, profile.Content!, helpers), null);
    }

    /// <summary>Builds the result for a package that breaks a rule.</summary>
    /// <param name="violation">The rule that is broken.</param>
    /// <returns>The result carrying the violation.</returns>
    private static PluginAgentCheck Refused(PluginAgentViolation violation)
    {
        return new PluginAgentCheck(null, violation);
    }

    /// <summary>Turns a read that did not succeed into the rule it breaks.</summary>
    /// <param name="outcome">How the read ended; never <see cref="PackageFileOutcome.Read"/>.</param>
    /// <param name="path">The package-relative path of the file, built from a constant or a checked name.</param>
    /// <param name="isProfile">Whether the file is the profile and not a helper.</param>
    /// <returns>The violation.</returns>
    private static PluginAgentViolation RefusalFor(PackageFileOutcome outcome, string path, bool isProfile)
    {
        return outcome switch
        {
            PackageFileOutcome.ReachedThroughLink => new(PluginAgentRule.LinkNotAllowed, $"{path} is reached through a link"),
            PackageFileOutcome.IsLink => new(PluginAgentRule.LinkNotAllowed, $"{path} is a link"),
            PackageFileOutcome.Missing when isProfile => new(PluginAgentRule.ProfileMissing, $"{path} does not exist in the plugin folder"),
            _ when isProfile => new(PluginAgentRule.ProfileInvalid, $"{path} is not a readable regular file of at most {PluginSshAgentLayout.MaxProfileBytes} bytes"),
            _ => new(PluginAgentRule.HelperInvalid, $"{path} is not a readable regular file of at most {PluginSshAgentLayout.MaxHelperBytes} bytes"),
        };
    }

    /// <summary>Checks the profile text: its format, its keys and the values the host does not leave to a package.</summary>
    /// <param name="bytes">The profile file bytes.</param>
    /// <param name="profileId">The profile id the manifest names.</param>
    /// <returns>The first rule the profile breaks, or <see langword="null"/>.</returns>
    private static PluginAgentViolation? CheckProfile(byte[] bytes, string profileId)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var problem = Parse(bytes, values);
        if (problem is not null)
        {
            return new PluginAgentViolation(PluginAgentRule.ProfileInvalid, $"{PluginSshAgentLayout.ProfileFile} {problem}");
        }

        foreach (var key in values.Keys.Order(StringComparer.Ordinal))
        {
            if (!ConfigKeys.Allowed.Contains(key))
            {
                return new PluginAgentViolation(PluginAgentRule.KeyNotAllowed, $"the profile sets '{key}', which the agent does not define");
            }
        }

        if (!values.TryGetValue(ConfigKeys.Provider, out var provider) || !string.Equals(provider, profileId, StringComparison.Ordinal))
        {
            return new PluginAgentViolation(PluginAgentRule.ProviderMismatch, $"the profile's {ConfigKeys.Provider} must be the sshAgentProfile id of the manifest");
        }

        if (values.TryGetValue(ConfigKeys.Binding, out var binding) && binding.Length > 0)
        {
            return new PluginAgentViolation(PluginAgentRule.BindingNotEmpty, $"the profile's {ConfigKeys.Binding} must be empty: the host sets it per server");
        }

        foreach (var key in new[] { ConfigKeys.AccountCheckCommand, ConfigKeys.HelperDir })
        {
            if (values.TryGetValue(key, out var path) && !IsInsideHelperDirectory(path))
            {
                return new PluginAgentViolation(PluginAgentRule.PathOutsideHelperDirectory, $"the profile's {key} must stay inside {ConfigKeys.StandardHelperDirectory}");
            }
        }

        return null;
    }

    /// <summary>
    /// Reads the profile the way the agent's configuration reader does, but strictly: one <c>[agent]</c> section, one
    /// <c>key = value</c> per line, lower case keys, no continuation line, no repeated key and no <c>%</c> (the reader
    /// would read it as a reference). Whole line comments and blank lines are skipped.
    /// </summary>
    /// <param name="bytes">The profile file bytes.</param>
    /// <param name="values">Receives each key with its trimmed value.</param>
    /// <returns>What is wrong, in fixed text, or <see langword="null"/> when the text is a valid profile.</returns>
    private static string? Parse(byte[] bytes, Dictionary<string, string> values)
    {
        string text;
        try
        {
            text = StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return "is not UTF-8 text";
        }

        if (text.Any(character => char.IsControl(character) && character is not ('\n' or '\r' or '\t')) || text.Contains('\uFEFF'))
        {
            return "holds a control character";
        }

        var inSection = false;
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || CommentMarkers.Contains(trimmed[0]))
            {
                continue;
            }

            if (char.IsWhiteSpace(line[0]))
            {
                return "has an indented line";
            }

            if (line[0] == '[')
            {
                if (inSection || !string.Equals(trimmed, SectionHeader, StringComparison.Ordinal))
                {
                    return $"has a section other than the one {SectionHeader}";
                }

                inSection = true;
                continue;
            }

            var problem = inSection ? ParseEntry(line, values) : $"has a line before the {SectionHeader} section";
            if (problem is not null)
            {
                return problem;
            }
        }

        return inSection ? null : $"has no {SectionHeader} section";
    }

    /// <summary>Reads one <c>key = value</c> line.</summary>
    /// <param name="line">The line, not indented.</param>
    /// <param name="values">Receives the key with its trimmed value.</param>
    /// <returns>What is wrong, in fixed text, or <see langword="null"/> when the line is a valid entry.</returns>
    private static string? ParseEntry(string line, Dictionary<string, string> values)
    {
        var delimiter = line.IndexOfAny(Delimiters);
        if (delimiter < 0 || line[delimiter] != '=')
        {
            return "has a line that is not 'key = value'";
        }

        var key = line[..delimiter].Trim();
        if (key.Length == 0 || !key.All(character => char.IsAsciiLetterLower(character) || character == '_'))
        {
            return "has a key that is not lower case letters and underscores";
        }

        var value = line[(delimiter + 1)..].Trim();
        if (value.Contains('%'))
        {
            return "has a value with a percent sign";
        }

        return values.TryAdd(key, value) ? null : "sets a key twice";
    }

    /// <summary>
    /// Whether a path value is empty or names the agent's standard helper folder or something below it, with plain
    /// segments only: no <c>.</c>, no <c>..</c>, no empty segment and no character a shell or a path could read differently.
    /// </summary>
    /// <param name="value">The value of <c>account_check_command</c> or <c>helper_dir</c>.</param>
    /// <returns><see langword="true"/> when the value stays inside the standard helper folder.</returns>
    private static bool IsInsideHelperDirectory(string value)
    {
        if (value.Length == 0 || string.Equals(value, ConfigKeys.StandardHelperDirectory, StringComparison.Ordinal))
        {
            return true;
        }

        var prefix = ConfigKeys.StandardHelperDirectory + PackagePathSeparator;
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return value[prefix.Length..].Split(PackagePathSeparator).All(IsPlainSegment);
    }

    /// <summary>Whether a path segment is a plain name: letters, digits, dot, underscore and hyphen, and not <c>.</c> or <c>..</c>.</summary>
    /// <param name="segment">The segment.</param>
    /// <returns><see langword="true"/> when the segment is a plain name.</returns>
    private static bool IsPlainSegment(string segment)
    {
        return segment.Length > 0
            && segment is not ("." or "..")
            && segment.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-');
    }

    /// <summary>Reads the helpers: every entry directly in the helper folder, each a plainly named regular file, within the count and size caps.</summary>
    /// <param name="root">The full path of the package folder.</param>
    /// <param name="violation">Receives the first rule the helpers break, or <see langword="null"/>.</param>
    /// <returns>The content of each helper by name, in ordinal order; empty when the package has no helper folder.</returns>
    private static SortedDictionary<string, byte[]> ReadHelpers(string root, out PluginAgentViolation? violation)
    {
        var helpers = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        violation = null;

        var folder = Path.Combine(root, PluginSshAgentLayout.HelperDirectory.Replace(PackagePathSeparator, Path.DirectorySeparatorChar));
        var info = new DirectoryInfo(folder);
        if (info.LinkTarget is not null)
        {
            violation = new PluginAgentViolation(PluginAgentRule.LinkNotAllowed, $"{PluginSshAgentLayout.HelperDirectory} is a link");
            return helpers;
        }

        if (File.Exists(folder))
        {
            violation = new PluginAgentViolation(PluginAgentRule.HelperInvalid, $"{PluginSshAgentLayout.HelperDirectory} is not a folder");
            return helpers;
        }

        if (!info.Exists)
        {
            return helpers;
        }

        string[] names;
        try
        {
            names = info.EnumerateFileSystemInfos().Select(entry => entry.Name).Order(StringComparer.Ordinal).ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Fixed text: an I/O message carries the absolute path of the folder.
            violation = new PluginAgentViolation(PluginAgentRule.HelperInvalid, $"{PluginSshAgentLayout.HelperDirectory} cannot be listed");
            return helpers;
        }

        if (names.Length > PluginSshAgentLayout.MaxHelperCount)
        {
            violation = new PluginAgentViolation(PluginAgentRule.TooManyHelpers, $"{PluginSshAgentLayout.HelperDirectory} holds more than {PluginSshAgentLayout.MaxHelperCount} helpers");
            return helpers;
        }

        foreach (var name in names)
        {
            // A name taken from the file system is untrusted until it passes the name rule, and is never echoed before.
            if (!PluginSshAgentLayout.IsValidHelperName(name))
            {
                violation = new PluginAgentViolation(PluginAgentRule.HelperInvalid, $"{PluginSshAgentLayout.HelperDirectory} holds a helper whose name is not allowed");
                return helpers;
            }

            var path = $"{PluginSshAgentLayout.HelperDirectory}/{name}";
            var read = PackageFileReader.Read(root, path, PluginSshAgentLayout.MaxHelperBytes);
            if (read.Outcome != PackageFileOutcome.Read)
            {
                violation = RefusalFor(read.Outcome, path, isProfile: false);
                return helpers;
            }

            helpers.Add(name, read.Content!);
        }

        return helpers;
    }
}
