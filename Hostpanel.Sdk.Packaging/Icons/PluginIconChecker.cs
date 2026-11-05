using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Hostpanel.Sdk.Packaging.Enums;

namespace Hostpanel.Sdk.Packaging.Icons;

/// <summary>
/// Reads and checks the icon a plugin manifest names, once, for a startup load and an install alike. The file must sit
/// inside the package, be a <c>.svg</c> or <c>.png</c> within the size cap, and hold what its extension says: a PNG
/// starts with the PNG signature and an <c>IHDR</c> chunk; an SVG is well-formed XML with no script, no
/// <c>foreignObject</c>, no event handler attribute and no reference that leaves the document. The bytes are returned so
/// a host can keep them in memory and serve the icon with no further file read.
/// </summary>
/// <remarks>
/// Every refusal detail is built from fixed text and, once the path is accepted, the package-relative path: a manifest
/// string that was refused, an absolute path, an exception message or a name taken from the icon file never reaches a log
/// line or a response.
/// </remarks>
public static partial class PluginIconChecker
{
    /// <summary>The largest icon file accepted, in bytes. An icon is a small image; the cap also bounds the XML parser.</summary>
    public const long MaxIconBytes = 256 * 1024;

    /// <summary>The extension of an SVG icon.</summary>
    private const string SvgExtension = ".svg";

    /// <summary>The extension of a PNG icon.</summary>
    private const string PngExtension = ".png";

    /// <summary>The media type of an SVG icon.</summary>
    private const string SvgContentType = "image/svg+xml";

    /// <summary>The media type of a PNG icon.</summary>
    private const string PngContentType = "image/png";

    /// <summary>The opening of a CSS comment.</summary>
    private const string CommentOpen = "/*";

    /// <summary>The closing of a CSS comment.</summary>
    private const string CommentClose = "*/";

    /// <summary>The separator of a package-relative path.</summary>
    private const char PackagePathSeparator = '/';

    /// <summary>How many bytes are read at a time; the read stops at the first chunk that passes the cap.</summary>
    private const int ReadChunkBytes = 8192;

    /// <summary>The root element name of an SVG document.</summary>
    private const string SvgRootName = "svg";

    /// <summary>The element that holds a stylesheet.</summary>
    private const string StyleElement = "style";

    /// <summary>The SVG element that animates an attribute.</summary>
    private const string AnimateElement = "animate";

    /// <summary>The SVG element that sets an attribute.</summary>
    private const string SetElement = "set";

    /// <summary>The attribute of an animation that names the attribute it changes.</summary>
    private const string AnimatedAttributeName = "attributeName";

    /// <summary>The attribute that references another resource.</summary>
    private const string HrefAttribute = "href";

    /// <summary>The prefix of every event handler attribute.</summary>
    private const string EventHandlerPrefix = "on";

    /// <summary>The separator between a namespace prefix and a local name.</summary>
    private const char NamePrefixSeparator = ':';

    /// <summary>The start of a reference that stays inside the document.</summary>
    private const char FragmentPrefix = '#';

    /// <summary>The character a CSS escape decodes to when its code point is not a character.</summary>
    private const string ReplacementCharacter = "�";

    /// <summary>The namespace of SVG elements.</summary>
    private const string SvgNamespace = "http://www.w3.org/2000/svg";

    /// <summary>The label every detail starts with when the manifest value was refused: fixed text, never the value.</summary>
    private const string RefusedPathLabel = "icon path";

    /// <summary>The first eight bytes of every PNG file.</summary>
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>The type of the first chunk of every PNG file, at bytes 12 to 15.</summary>
    private static readonly byte[] PngHeaderChunk = [0x49, 0x48, 0x44, 0x52];

    /// <summary>Elements that run code, embed foreign content or load another document, whatever their attributes.</summary>
    private static readonly HashSet<string> ForbiddenElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "foreignObject", "iframe", "object", "embed", "audio", "video", "canvas", "handler", "listener",
    };

    /// <summary>Matches one segment of a package-relative path: lowercase letters, digits, dot, hyphen and underscore, not starting with a dot.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\A[a-z0-9][a-z0-9_.-]*\z", RegexOptions.CultureInvariant, RegexLimits.TimeoutMilliseconds)]
    internal static partial Regex PathSegmentPattern();

    /// <summary>Matches a <c>url(...)</c> reference whose target is not a fragment of the same document.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"url\s*\((?>\s*)(?!['""]?\s*#)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, RegexLimits.TimeoutMilliseconds)]
    internal static partial Regex ExternalUrlPattern();

    /// <summary>Matches an <c>@import</c> rule, a script URL or an <c>expression(</c> in a style.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"@import|javascript\s*:|expression\s*\(|behavior\s*:|-moz-binding", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, RegexLimits.TimeoutMilliseconds)]
    internal static partial Regex ActiveStylePattern();

    /// <summary>
    /// Matches a CSS escape: a backslash and one to six hex digits with an optional closing white space, or a backslash
    /// and any one other character.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"\\(?:([0-9a-fA-F]{1,6})[ \t\r\n\f]?|(.))", RegexOptions.CultureInvariant | RegexOptions.Singleline, RegexLimits.TimeoutMilliseconds)]
    internal static partial Regex CssEscapePattern();

    /// <summary>The patterns production runs: the generated ones, all under <see cref="RegexLimits.TimeoutMilliseconds"/>.</summary>
    private static readonly IconPatterns DefaultPatterns =
        new(PathSegmentPattern(), ExternalUrlPattern(), ActiveStylePattern(), CssEscapePattern());

    /// <summary>Reads the icon the manifest names, if any, and checks it against the icon rules.</summary>
    /// <param name="packageDir">The package folder: the installed folder at load, the unpacked staging folder at install.</param>
    /// <param name="iconPath">The manifest's <c>icon</c> member, or <see langword="null"/> or empty when it names none.</param>
    /// <returns>The checked icon, nothing when the manifest names none, or the first rule the icon breaks.</returns>
    public static PluginIconCheck Check(string packageDir, string? iconPath)
    {
        return Check(packageDir, iconPath, DefaultPatterns);
    }

    /// <summary>Reads the icon the manifest names, if any, and checks it with the given patterns.</summary>
    /// <param name="packageDir">The package folder.</param>
    /// <param name="iconPath">The manifest's <c>icon</c> member, or <see langword="null"/> or empty when it names none.</param>
    /// <param name="patterns">The regular expressions to run; a test passes ones with a tiny timeout.</param>
    /// <returns>The checked icon, nothing when the manifest names none, or the first rule the icon breaks.</returns>
    internal static PluginIconCheck Check(string packageDir, string? iconPath, IconPatterns patterns)
    {
        ArgumentException.ThrowIfNullOrEmpty(packageDir);

        if (string.IsNullOrEmpty(iconPath))
        {
            return new PluginIconCheck(null, null);
        }

        // A refused value is never echoed: the manifest string is untrusted until it passes the path rule.
        if (!IsPackageRelative(iconPath, patterns))
        {
            return Invalid(RefusedPathLabel, $"is not a package-relative {SvgExtension} or {PngExtension} path");
        }

        var label = $"icon '{iconPath}'";
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(packageDir));
        var full = Path.GetFullPath(Path.Combine(root, iconPath));
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return Invalid(label, "leaves the plugin folder");
        }

        var located = CheckLocation(root, iconPath, full, label);
        if (located is not null)
        {
            return new PluginIconCheck(null, located);
        }

        try
        {
            // One handle for the size and the read, so the length checked is the length of the file that is read.
            using var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (!stream.CanSeek || stream.Length > MaxIconBytes)
            {
                return Invalid(label, $"is not a regular file of at most {MaxIconBytes} bytes");
            }

            // The path could have been swapped for a link between the first look and the open: look again, now that the
            // handle is held. The public API gives no final path of a handle, so the path is checked a second time.
            var relocated = CheckLocation(root, iconPath, full, label);
            if (relocated is not null)
            {
                return new PluginIconCheck(null, relocated);
            }

            var bytes = ReadBounded(stream);
            if (bytes is null)
            {
                return Invalid(label, $"is not a regular file of at most {MaxIconBytes} bytes");
            }

            var isSvg = iconPath.EndsWith(SvgExtension, StringComparison.Ordinal);
            var problem = isSvg ? CheckSvg(bytes, patterns) : CheckPng(bytes);
            if (problem is not null)
            {
                return Invalid(label, problem);
            }

            var tag = $"\"{Convert.ToHexStringLower(SHA256.HashData(bytes))[..32]}\"";
            return new PluginIconCheck(new PluginIcon(isSvg ? SvgContentType : PngContentType, bytes, tag), null);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // Removed between the look and the open: the one case of an unreadable file that is a missing one.
            return new PluginIconCheck(
                null, new PluginIconViolation(PluginIconRule.Missing, $"{label} does not exist in the plugin folder"));
        }
        catch (RegexMatchTimeoutException)
        {
            // A text that keeps a pattern busy for that long is hostile, not an icon.
            return Invalid(label, "takes too long to check");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Fixed text: an I/O message carries the absolute path of the file. A file that exists but cannot be read, or
            // that is a device or a pipe, is not a valid icon; only a missing file is a missing one.
            return Invalid(label, "cannot be read as a regular file");
        }
    }

    /// <summary>
    /// Whether a style text or an attribute value loads or runs something outside the image. It is read twice: as written,
    /// and as a stylesheet reads it, with comments read as a space and CSS escapes decoded, so an escaped <c>url</c> or an <c>@import</c> split by
    /// a comment does not get past the patterns.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <param name="patterns">The regular expressions to run.</param>
    /// <returns><see langword="true"/> when either reading matches a pattern.</returns>
    private static bool LoadsOutside(string value, IconPatterns patterns)
    {
        if (patterns.ActiveStyle.IsMatch(value) || patterns.ExternalUrl.IsMatch(value))
        {
            return true;
        }

        // A comment that never closes is refused outright: a stylesheet reader would swallow the rest of the text, and
        // guessing what it hides is how a filter is bypassed.
        var uncommented = StripComments(value);
        if (uncommented is null)
        {
            return true;
        }

        var decoded = patterns.CssEscape.Replace(uncommented, DecodeEscape);
        return patterns.ActiveStyle.IsMatch(decoded) || patterns.ExternalUrl.IsMatch(decoded);
    }

    /// <summary>
    /// Replaces every CSS comment with a space, as a stylesheet reads it. A hand scan, not a pattern: it visits each
    /// character once whatever the text holds, so no input makes it slow.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <returns>The text with comments read as a space, or <see langword="null"/> when a comment is never closed.</returns>
    private static string? StripComments(string value)
    {
        var result = new StringBuilder(value.Length);
        var position = 0;
        while (position < value.Length)
        {
            var open = value.IndexOf(CommentOpen, position, StringComparison.Ordinal);
            if (open < 0)
            {
                result.Append(value, position, value.Length - position);
                break;
            }

            result.Append(value, position, open - position).Append(' ');
            var close = value.IndexOf(CommentClose, open + CommentOpen.Length, StringComparison.Ordinal);
            if (close < 0)
            {
                return null;
            }

            position = close + CommentClose.Length;
        }

        return result.ToString();
    }

    /// <summary>Decodes one CSS escape to the character it stands for.</summary>
    /// <param name="match">The escape, from <see cref="CssEscapePattern"/>.</param>
    /// <returns>The character, or the replacement character for a code point that is not one.</returns>
    private static string DecodeEscape(Match match)
    {
        if (!match.Groups[1].Success)
        {
            return match.Groups[2].Value;
        }

        var codePoint = Convert.ToInt32(match.Groups[1].Value, 16);
        return codePoint is 0 or > 0x10FFFF or (>= 0xD800 and <= 0xDFFF) ? ReplacementCharacter : char.ConvertFromUtf32(codePoint);
    }

    /// <summary>Builds the result for an icon that breaks <see cref="PluginIconRule.Invalid"/>.</summary>
    /// <param name="label">The fixed label of the icon, for the log.</param>
    /// <param name="problem">What is wrong, without a host path.</param>
    /// <returns>The result carrying the violation.</returns>
    private static PluginIconCheck Invalid(string label, string problem)
    {
        return new PluginIconCheck(null, new PluginIconViolation(PluginIconRule.Invalid, $"{label} {problem}"));
    }

    /// <summary>Checks that the path has plain segments and ends in a supported extension.</summary>
    /// <param name="path">The manifest value.</param>
    /// <param name="patterns">The regular expressions to run.</param>
    /// <returns><see langword="true"/> when the path may be opened inside the package; a pattern that times out refuses it.</returns>
    internal static bool IsPackageRelative(string path, IconPatterns patterns)
    {
        if (!path.EndsWith(SvgExtension, StringComparison.Ordinal) && !path.EndsWith(PngExtension, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            return path.Split(PackagePathSeparator).All(segment => patterns.PathSegment.IsMatch(segment));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Checks that the file exists and is reached without a link: no folder between the package folder and the file, and
    /// not the file, is a symbolic link or junction. A link would let the icon read a file outside the package.
    /// </summary>
    /// <param name="root">The full path of the package folder.</param>
    /// <param name="path">The package-relative path, already checked by <see cref="IsPackageRelative"/>.</param>
    /// <param name="full">The full path of the file.</param>
    /// <param name="label">The label of the icon, for the log.</param>
    /// <returns>The violation, or <see langword="null"/> when the file exists and nothing on the way is a link.</returns>
    private static PluginIconViolation? CheckLocation(string root, string path, string full, string label)
    {
        var segments = path.Split(PackagePathSeparator);
        var current = root;
        foreach (var segment in segments.Take(segments.Length - 1))
        {
            current = Path.Combine(current, segment);
            var folder = new DirectoryInfo(current);
            if (folder.LinkTarget is not null)
            {
                return new PluginIconViolation(PluginIconRule.Invalid, $"{label} is reached through a link");
            }

            if (!folder.Exists)
            {
                return new PluginIconViolation(PluginIconRule.Missing, $"{label} does not exist in the plugin folder");
            }
        }

        var file = new FileInfo(full);
        if (file.LinkTarget is not null)
        {
            return new PluginIconViolation(PluginIconRule.Invalid, $"{label} is a link");
        }

        return file.Exists
            ? null
            : new PluginIconViolation(PluginIconRule.Missing, $"{label} does not exist in the plugin folder");
    }

    /// <summary>Reads the stream to its end, stopping with <see langword="null"/> as soon as it passes the cap.</summary>
    /// <param name="stream">The open icon file.</param>
    /// <returns>The bytes, or <see langword="null"/> when the file is over <see cref="MaxIconBytes"/>.</returns>
    private static byte[]? ReadBounded(Stream stream)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[ReadChunkBytes];
        int read;
        while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (buffer.Length + read > MaxIconBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    /// <summary>Checks the PNG signature and that the first chunk is <c>IHDR</c>.</summary>
    /// <param name="bytes">The file bytes.</param>
    /// <returns>What is wrong, or <see langword="null"/>.</returns>
    private static string? CheckPng(byte[] bytes)
    {
        var isPng = bytes.Length >= 16
            && bytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature)
            && bytes.AsSpan(12, PngHeaderChunk.Length).SequenceEqual(PngHeaderChunk);
        return isPng ? null : "is not a PNG image";
    }

    /// <summary>
    /// Parses the SVG as XML (DTDs prohibited, no resolver) and walks every node. The text of a <c>style</c> element is
    /// gathered across all its text, CDATA and child nodes and checked as one string, so a pattern split over nodes is
    /// still found; text elsewhere (a <c>title</c>, a <c>desc</c>) is not a stylesheet and is not scanned. A host also
    /// serves the icon with a restrictive content security policy; this check keeps a hostile file out of the package altogether.
    /// </summary>
    /// <param name="bytes">The file bytes.</param>
    /// <param name="patterns">The regular expressions to run.</param>
    /// <returns>What is wrong, or <see langword="null"/>.</returns>
    private static string? CheckSvg(byte[] bytes, IconPatterns patterns)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,

            // Characters never outnumber bytes, so this cannot refuse a file the byte cap let in; it only bounds the parser.
            MaxCharactersInDocument = MaxIconBytes + 1,
            IgnoreComments = true,
            CloseInput = true,
        };

        try
        {
            using var reader = XmlReader.Create(new MemoryStream(bytes, writable: false), settings);
            var sawRoot = false;
            var styleDepth = 0;
            var styleText = new StringBuilder();
            while (reader.Read())
            {
                var problem = CheckNode(reader, ref sawRoot, patterns);
                if (problem is not null)
                {
                    return problem;
                }

                problem = TrackStyle(reader, ref styleDepth, styleText, patterns);
                if (problem is not null)
                {
                    return problem;
                }
            }

            return sawRoot ? null : "is not an SVG document";
        }
        catch (Exception ex) when (ex is XmlException or DecoderFallbackException)
        {
            // Fixed text: a parser message quotes the document.
            return "is not well-formed SVG";
        }
    }

    /// <summary>
    /// Gathers the text content of each outermost <c>style</c> element, descendants included, and checks it when the
    /// element closes.
    /// </summary>
    /// <param name="reader">The XML reader, on the node just read.</param>
    /// <param name="styleDepth">How many <c>style</c> elements are open.</param>
    /// <param name="styleText">The text gathered for the outermost open <c>style</c> element.</param>
    /// <param name="patterns">The regular expressions to run.</param>
    /// <returns>What is wrong with the stylesheet, or <see langword="null"/>.</returns>
    private static string? TrackStyle(XmlReader reader, ref int styleDepth, StringBuilder styleText, IconPatterns patterns)
    {
        switch (reader.NodeType)
        {
            case XmlNodeType.Text:
            case XmlNodeType.CDATA:
            case XmlNodeType.Whitespace:
            case XmlNodeType.SignificantWhitespace:
                if (styleDepth > 0)
                {
                    styleText.Append(reader.Value);
                }

                return null;
            case XmlNodeType.Element:
                if (!reader.IsEmptyElement && reader.LocalName.Equals(StyleElement, StringComparison.OrdinalIgnoreCase))
                {
                    styleDepth++;
                }

                return null;
            case XmlNodeType.EndElement:
                if (styleDepth > 0 && reader.LocalName.Equals(StyleElement, StringComparison.OrdinalIgnoreCase))
                {
                    styleDepth--;
                    if (styleDepth == 0)
                    {
                        var text = styleText.ToString();
                        styleText.Clear();
                        return LoadsOutside(text, patterns) ? "holds a style that loads or runs something outside the image" : null;
                    }
                }

                return null;
            default:
                return null;
        }
    }

    /// <summary>Checks one node the reader stands on, apart from stylesheet text, which <see cref="TrackStyle"/> gathers.</summary>
    /// <param name="reader">The XML reader, on the node.</param>
    /// <param name="sawRoot">Whether the root element has been seen; set on the first element.</param>
    /// <param name="patterns">The regular expressions to run.</param>
    /// <returns>What is wrong with the node, or <see langword="null"/>.</returns>
    private static string? CheckNode(XmlReader reader, ref bool sawRoot, IconPatterns patterns)
    {
        switch (reader.NodeType)
        {
            case XmlNodeType.ProcessingInstruction:
                return "holds a processing instruction";
            case XmlNodeType.Element:
                if (!sawRoot)
                {
                    sawRoot = true;
                    if (reader.LocalName != SvgRootName || reader.NamespaceURI != SvgNamespace)
                    {
                        return "is not an SVG document";
                    }
                }

                // The element name is not echoed: only the fixed text, so a document cannot put its own text in a detail.
                if (ForbiddenElements.Contains(reader.LocalName))
                {
                    return "holds an element that runs code or embeds other content";
                }

                return CheckAttributes(reader, patterns);
            default:
                return null;
        }
    }

    /// <summary>Checks every attribute of the element the reader stands on.</summary>
    /// <param name="reader">The XML reader, on an element.</param>
    /// <param name="patterns">The regular expressions to run.</param>
    /// <returns>What is wrong, or <see langword="null"/>.</returns>
    private static string? CheckAttributes(XmlReader reader, IconPatterns patterns)
    {
        string? animatedAttribute = null;
        var isAnimation = reader.LocalName is AnimateElement or SetElement;
        var hasAttributes = reader.MoveToFirstAttribute();
        while (hasAttributes)
        {
            var name = reader.LocalName;
            var value = reader.Value;
            if (name.StartsWith(EventHandlerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return "holds an event handler attribute";
            }

            if (name.Equals(HrefAttribute, StringComparison.OrdinalIgnoreCase) && !value.StartsWith(FragmentPrefix))
            {
                return "references something outside the image";
            }

            if (LoadsOutside(value, patterns))
            {
                return "holds an attribute that loads or runs something outside the image";
            }

            if (isAnimation && name == AnimatedAttributeName)
            {
                animatedAttribute = value;
            }

            hasAttributes = reader.MoveToNextAttribute();
        }

        reader.MoveToElement();

        // An animation can set href or an event handler later, so the attribute it targets is checked as well.
        if (animatedAttribute is not null && TargetsReferenceOrHandler(animatedAttribute))
        {
            return "animates an attribute that could load something outside the image";
        }

        return null;
    }

    /// <summary>Whether the attribute an animation changes is a reference or an event handler.</summary>
    /// <param name="attributeName">The <c>attributeName</c> value, which may carry white space and a namespace prefix.</param>
    /// <returns><see langword="true"/> when the target is <c>href</c> or starts with <c>on</c>.</returns>
    private static bool TargetsReferenceOrHandler(string attributeName)
    {
        var trimmed = attributeName.Trim();
        var local = trimmed[(trimmed.LastIndexOf(NamePrefixSeparator) + 1)..];
        return trimmed.Contains(HrefAttribute, StringComparison.OrdinalIgnoreCase)
            || local.StartsWith(EventHandlerPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
