namespace Hostpanel.Sdk.Plugins.Store;

/// <summary>
/// The rules of a key of the plugin store and of a prefix for <see cref="IPluginStoreAdvanced.ListKeysAsync"/>, in one
/// place: a key is 1 to <see cref="MaxLength"/> characters of ASCII letters, digits, <c>.</c>, <c>-</c>, <c>_</c> and
/// <c>:</c>; a prefix follows the same rules except that it may be empty. Both are case sensitive and compared
/// ordinally. The host refuses a key that breaks them, and <see cref="InMemoryPluginStore"/> does the same, so a test
/// finds the mistake before the host does.
/// </summary>
public static class PluginStoreKeys
{
    /// <summary>The most characters in a key or a prefix.</summary>
    public const int MaxLength = 200;

    /// <summary>Tells whether a text is a valid key.</summary>
    /// <param name="key">The text to check.</param>
    /// <returns><see langword="true"/> for 1 to <see cref="MaxLength"/> allowed characters.</returns>
    public static bool IsValidKey(string? key)
    {
        return key is { Length: > 0 and <= MaxLength } && key.All(IsAllowed);
    }

    /// <summary>Tells whether a text is a valid prefix for a listing.</summary>
    /// <param name="prefix">The text to check.</param>
    /// <returns><see langword="true"/> for 0 to <see cref="MaxLength"/> allowed characters.</returns>
    public static bool IsValidPrefix(string? prefix)
    {
        return prefix is { Length: <= MaxLength } && prefix.All(IsAllowed);
    }

    /// <summary>Tells whether a character may appear in a key.</summary>
    /// <param name="character">The character.</param>
    /// <returns><see langword="true"/> for an ASCII letter or digit, <c>.</c>, <c>-</c>, <c>_</c> or
    /// <c>:</c>.</returns>
    private static bool IsAllowed(char character)
    {
        return char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or ':';
    }
}
