using System.Text.RegularExpressions;

namespace Hostpanel.Sdk.Packaging.Icons;

/// <summary>
/// The regular expressions the icon check runs. Production uses <see cref="PluginIconChecker"/>'s own generated ones; a test
/// passes patterns with a tiny timeout to prove a timeout makes the icon invalid.
/// </summary>
/// <param name="PathSegment">Matches one segment of a package-relative path.</param>
/// <param name="ExternalUrl">Matches a <c>url(...)</c> reference that leaves the document.</param>
/// <param name="ActiveStyle">Matches an import, a script URL or another active construct in a style.</param>
/// <param name="CssEscape">Matches a CSS escape.</param>
internal sealed record IconPatterns(Regex PathSegment, Regex ExternalUrl, Regex ActiveStyle, Regex CssEscape);
