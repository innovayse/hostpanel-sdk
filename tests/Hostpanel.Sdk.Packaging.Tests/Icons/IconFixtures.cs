using Hostpanel.Sdk.Packaging.Icons;
using Hostpanel.Sdk.Packaging.Tests.Support;

namespace Hostpanel.Sdk.Packaging.Tests.Icons;

/// <summary>The files and helpers the icon checker tests share, so a test class holds only tests.</summary>
public static class IconFixtures
{
    /// <summary>The opening of the SVG root element, with its namespace, up to and including the closing angle bracket.</summary>
    public const string SvgOpen = "<svg xmlns=\"http://www.w3.org/2000/svg\">";

    /// <summary>A small valid SVG.</summary>
    public const string GoodSvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><rect width=\"1\" height=\"1\" fill=\"#fff\"/></svg>";

    /// <summary>The smallest bytes the checker takes for a PNG: signature, chunk length, <c>IHDR</c> and a little more.</summary>
    public static readonly byte[] GoodPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01,
    ];

    /// <summary>Wraps markup in an SVG root element.</summary>
    /// <param name="inner">The markup inside the root.</param>
    /// <returns>The whole document.</returns>
    public static string Svg(string inner)
    {
        return SvgOpen + inner + "</svg>";
    }

    /// <summary>Checks a single text icon file.</summary>
    /// <param name="relativePath">Where the icon is written, and what the manifest names.</param>
    /// <param name="content">The icon file content.</param>
    /// <returns>The check result.</returns>
    public static PluginIconCheck CheckFile(string relativePath, string content)
    {
        using var folder = new TempFolder();
        folder.Write(relativePath, content);
        return PluginIconChecker.Check(folder.Path, relativePath);
    }

    /// <summary>Checks a single binary icon file.</summary>
    /// <param name="relativePath">Where the icon is written, and what the manifest names.</param>
    /// <param name="content">The icon file bytes.</param>
    /// <returns>The check result.</returns>
    public static PluginIconCheck CheckBytes(string relativePath, byte[] content)
    {
        using var folder = new TempFolder();
        folder.Write(relativePath, content);
        return PluginIconChecker.Check(folder.Path, relativePath);
    }

    /// <summary>Checks <c>icon.svg</c> holding the given markup inside an SVG root.</summary>
    /// <param name="inner">The markup inside the root.</param>
    /// <returns>The check result.</returns>
    public static PluginIconCheck CheckSvg(string inner)
    {
        return CheckFile("icon.svg", Svg(inner));
    }

    /// <summary>Makes a byte array of an exact length that starts with the given bytes and is padded with zeros.</summary>
    /// <param name="start">The first bytes.</param>
    /// <param name="length">The length wanted.</param>
    /// <returns>The padded bytes.</returns>
    public static byte[] Pad(byte[] start, long length)
    {
        var bytes = new byte[length];
        start.CopyTo(bytes, 0);
        return bytes;
    }
}
