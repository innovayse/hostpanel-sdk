namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>
/// The rules for <see cref="PluginManifest.Tags"/>, the keywords a plugin registry uses to find a plugin. A tag is
/// <see cref="MinLength"/> to <see cref="MaxLength"/> lowercase ASCII letters, digits and hyphens, with no leading
/// <c>#</c> (the <c>#</c> is not in the allowed set), and a manifest lists at most <see cref="MaxCount"/> of them.
/// </summary>
public static class PluginTags
{
    /// <summary>The fewest characters one tag may have.</summary>
    public const int MinLength = 2;

    /// <summary>The most characters one tag may have.</summary>
    public const int MaxLength = 30;

    /// <summary>The most tags a manifest may list.</summary>
    public const int MaxCount = 10;

    /// <summary>Tells whether one tag is well formed; it does not look at the other tags of the list.</summary>
    /// <param name="tag">The tag to check.</param>
    /// <returns><see langword="true"/> when the tag may appear in a <c>tags</c> list.</returns>
    public static bool IsWellFormed(string? tag)
    {
        if (tag is null || tag.Length < MinLength || tag.Length > MaxLength)
        {
            return false;
        }

        foreach (var c in tag)
        {
            var allowed = c is >= 'a' and <= 'z' || c is >= '0' and <= '9' || c == '-';
            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }
}
