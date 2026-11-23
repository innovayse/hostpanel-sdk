namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>
/// Where a plugin that names an <see cref="PluginManifest.SshAgentProfile"/> keeps the files of its SSH key agent
/// profile inside its package, and the limits those files meet. The profile is one <c>.conf</c> file in the agent's
/// own configuration format, and the helpers are the small programs the agent runs for the profile. The paths are
/// relative to the package root and written with forward slashes whatever system the package was built on.
/// </summary>
public static class PluginSshAgentLayout
{
    /// <summary>The package path of the profile file.</summary>
    public const string ProfileFile = "agent/profile.conf";

    /// <summary>The package folder that holds the helpers, one file each, directly inside it.</summary>
    public const string HelperDirectory = "agent/helpers";

    /// <summary>The profile id the host keeps for its own default profile; a plugin cannot name it.</summary>
    public const string ReservedProfile = "generic-linux";

    /// <summary>The largest profile file, in bytes (8 KiB).</summary>
    public const int MaxProfileBytes = 8 * 1024;

    /// <summary>The largest number of helpers a package may carry.</summary>
    public const int MaxHelperCount = 32;

    /// <summary>The largest helper file, in bytes (64 KiB).</summary>
    public const int MaxHelperBytes = 64 * 1024;

    /// <summary>The longest profile id, in characters.</summary>
    public const int MaxProfileIdLength = 32;

    /// <summary>The longest helper name, in characters.</summary>
    public const int MaxHelperNameLength = 64;

    /// <summary>
    /// Tells whether a text is a profile id: lower case letters and digits, in words joined by single hyphens, at most
    /// <see cref="MaxProfileIdLength"/> characters. <see cref="ReservedProfile"/> has this shape too, so the shape alone
    /// does not decide whether a plugin may take it.
    /// </summary>
    /// <param name="id">The text to check.</param>
    /// <returns><see langword="true"/> when the text has the shape of a profile id.</returns>
    public static bool IsValidProfileId(string? id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > MaxProfileIdLength)
        {
            return false;
        }

        var previousWasHyphen = true;
        foreach (var character in id)
        {
            if (character == '-')
            {
                if (previousWasHyphen)
                {
                    return false;
                }

                previousWasHyphen = true;
            }
            else if (char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character))
            {
                previousWasHyphen = false;
            }
            else
            {
                return false;
            }
        }

        return !previousWasHyphen;
    }

    /// <summary>
    /// Tells whether a text is a helper name: one letter or digit, then up to <see cref="MaxHelperNameLength"/> minus one
    /// letters, digits, dots, underscores or hyphens. A name never starts with a dot or holds a separator, so it cannot
    /// name a parent folder or a hidden file.
    /// </summary>
    /// <param name="name">The text to check.</param>
    /// <returns><see langword="true"/> when the text has the shape of a helper name.</returns>
    public static bool IsValidHelperName(string? name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > MaxHelperNameLength || !char.IsAsciiLetterOrDigit(name[0]))
        {
            return false;
        }

        return name.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-');
    }
}
