using System.Text.RegularExpressions;
using Hostpanel.Sdk.Packaging.Naming;

namespace Hostpanel.Sdk.Packaging.Manifest;

/// <summary>The path rule of a manifest route and page: a leading slash, then lowercase literal segments and <c>{parameters}</c>.</summary>
public static partial class PluginRoutePath
{
    /// <summary>
    /// Matches one literal route segment: lowercase letters, digits, hyphen and underscore, with nothing after it, not even a newline.
    /// The shape is the one every literal segment of an address has: <see cref="PluginSegment.Pattern"/>.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(PluginSegment.Pattern, RegexOptions.CultureInvariant, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex LiteralSegmentPattern();

    /// <summary>Matches one route parameter segment such as <c>{id}</c>, with nothing after it, not even a newline.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\A\{[A-Za-z][A-Za-z0-9]*\}\z", RegexOptions.CultureInvariant, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex ParameterSegmentPattern();

    /// <summary>
    /// Splits a route path into segments and checks each one, returning the template with every parameter name
    /// replaced by a placeholder, so <c>/a/{id}</c> and <c>/a/{name}</c> count as the same route.
    /// </summary>
    /// <param name="path">The declared path.</param>
    /// <param name="template">The normalised template, when the path is valid.</param>
    /// <returns><see langword="true"/> when the path starts with a slash and every segment is a lowercase literal or a parameter.</returns>
    public static bool TryNormalize(string? path, out string template)
    {
        template = string.Empty;
        if (string.IsNullOrEmpty(path) || path[0] != '/')
        {
            return false;
        }

        var normalized = new List<string>();
        foreach (var segment in path[1..].Split('/'))
        {
            if (ParameterSegmentPattern().IsMatch(segment))
            {
                normalized.Add("{}");
            }
            else if (LiteralSegmentPattern().IsMatch(segment))
            {
                normalized.Add(segment);
            }
            else
            {
                // Empty, "." or "..", uppercase, or any other character.
                return false;
            }
        }

        template = "/" + string.Join('/', normalized);
        return true;
    }
}
