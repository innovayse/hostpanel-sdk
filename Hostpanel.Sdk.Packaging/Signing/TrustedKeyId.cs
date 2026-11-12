using System.Text.RegularExpressions;

namespace Hostpanel.Sdk.Packaging.Signing;

/// <summary>The shape of a signing key id, shared by a host's trusted key configuration, the signature file and the signing tool.</summary>
public static partial class TrustedKeyId
{
    /// <summary>The longest key id, in characters, as for <see cref="Naming.PluginIdentifier.MaxLength"/>; checked in <see cref="IsValid"/> beside <see cref="Pattern"/>, which holds the shape only.</summary>
    public const int MaxLength = 64;

    /// <summary>
    /// Lower-case letters, digits, dot, hyphen and underscore, starting with a letter or digit (the length is checked in <see cref="IsValid"/>).
    /// Narrow on purpose: the id is written into the signed payload on its own line, so it must never hold a newline
    /// or a space. The end is <c>\z</c>, not <c>$</c>, because <c>$</c> also matches before a trailing newline.
    /// </summary>
    /// <returns>The pattern.</returns>
    [GeneratedRegex(@"\A[a-z0-9][a-z0-9._-]*\z", RegexOptions.CultureInvariant, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex Pattern();

    /// <summary>Whether a text is a valid key id.</summary>
    /// <param name="keyId">The candidate.</param>
    /// <returns><see langword="true"/> when it is 1 to <see cref="MaxLength"/> characters of the allowed set.</returns>
    public static bool IsValid(string? keyId)
    {
        return keyId is not null && keyId.Length <= MaxLength && Pattern().IsMatch(keyId);
    }
}
