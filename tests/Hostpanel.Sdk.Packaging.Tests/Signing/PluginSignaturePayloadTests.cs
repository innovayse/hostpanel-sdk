using System.Text;
using Hostpanel.Sdk.Packaging.Signing;
using Hostpanel.Sdk.Packaging.Tests.Support;

namespace Hostpanel.Sdk.Packaging.Tests.Signing;

/// <summary>
/// Tests of <see cref="PluginSignaturePayload"/>, including the golden test that pins the v1 format byte for byte: a host
/// that builds the payload for the same files must produce exactly <see cref="GoldenPayload"/>.
/// </summary>
public sealed class PluginSignaturePayloadTests
{
    /// <summary>The key id the golden payload names.</summary>
    private const string GoldenKeyId = "demo-key";

    /// <summary>
    /// The payload of the golden package, written out. Each hash is the SHA-256 of the file written by
    /// <see cref="GoldenPackage.Write"/>; the paths are sorted with ordinal comparison, so an upper-case name sorts first.
    /// A host test that builds the payload of the same files with its own code must assert this exact text.
    /// </summary>
    public const string GoldenPayload =
        "innovayse-plugin-signature-v1\n"
        + "key demo-key\n"
        + "ae8f5080a348fbfeb2c776957979728034cdf3eee0160c8667ee66daa8440a48 Zed.txt\n"
        + "d4dc56669143034f31aa309635d4113d9ad76a02b1739da22c965ed2049be9e6 assets/icon.svg\n"
        + "9a3a45d01531a20e89ac6ae10b0b0beb0492acd7216a368aa062d1a5fecaf9cd lib/Demo.dll\n"
        + "2b67bf9bbf4bbcaf60ea7df1f970eee71425d927e00edb4a1de3ddc28ecc0991 plugin.json\n";

    /// <summary>The payload of the golden package is exactly the v1 text, as UTF-8 bytes with no byte order mark.</summary>
    [Fact]
    public void Build_GoldenPackage_EqualsTheV1Text()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);

        var payload = PluginSignaturePayload.Build(folder.Path, GoldenKeyId);

        payload.Should().Equal(new UTF8Encoding(false).GetBytes(GoldenPayload));
        payload.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
    }

    /// <summary>The payload does not depend on the order files were written in.</summary>
    [Fact]
    public void Build_WriteOrder_DoesNotMatter()
    {
        using var first = new TempFolder();
        GoldenPackage.Write(first);
        using var second = new TempFolder();
        second.Write("Zed.txt", "zed");
        second.Write("assets/icon.svg", "<svg/>");
        second.Write("lib/Demo.dll", "binary");
        second.Write("plugin.json", "{\"id\":\"demo\"}");

        PluginSignaturePayload.Build(first.Path, GoldenKeyId)
            .Should().Equal(PluginSignaturePayload.Build(second.Path, GoldenKeyId));
    }

    /// <summary>Only the root signature file is left out: one in a sub-folder is a file like any other.</summary>
    [Fact]
    public void Build_NestedSignatureFile_IsListed()
    {
        using var folder = new TempFolder();
        folder.Write("plugin.json", "{}");
        folder.Write("sub/plugin.sig", "x");

        var text = Encoding.UTF8.GetString(PluginSignaturePayload.Build(folder.Path, GoldenKeyId));

        text.Should().Contain(" sub/plugin.sig\n");
        text.Split('\n').Where(line => line.EndsWith(" plugin.sig", StringComparison.Ordinal)).Should().BeEmpty();
    }

    /// <summary>A change of one byte of one file changes the payload; so does a different key id.</summary>
    [Fact]
    public void Build_ChangedFileOrKeyId_ChangesThePayload()
    {
        using var folder = new TempFolder();
        GoldenPackage.Write(folder);
        var before = PluginSignaturePayload.Build(folder.Path, GoldenKeyId);

        PluginSignaturePayload.Build(folder.Path, "other-key").Should().NotEqual(before);

        folder.Write("lib/Demo.dll", "binarY");
        PluginSignaturePayload.Build(folder.Path, GoldenKeyId).Should().NotEqual(before);
    }

    /// <summary>A key id outside the allowed shape is refused before anything is read.</summary>
    /// <param name="keyId">The invalid key id.</param>
    [Theory]
    [InlineData("")]
    [InlineData("Upper")]
    [InlineData("has space")]
    [InlineData("line\nbreak")]
    public void Build_InvalidKeyId_Throws(string keyId)
    {
        using var folder = new TempFolder();
        folder.Write("plugin.json", "{}");

        var act = () => PluginSignaturePayload.Build(folder.Path, keyId);

        act.Should().Throw<InvalidDataException>();
    }

    /// <summary>The header names the format version, so a later format is a new header and not a silent clash.</summary>
    [Fact]
    public void Header_NamesVersionOne()
    {
        PluginSignaturePayload.Header.Should().Be("innovayse-plugin-signature-v1");
    }

    /// <summary>A file name that holds a line feed would let one file stand for two lines, so it cannot be signed (where the system can create it).</summary>
    [Fact]
    public void Build_ControlCharacterInName_Throws()
    {
        using var folder = new TempFolder();
        try
        {
            folder.Write("bad\nname.txt", "x");
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException or UnauthorizedAccessException)
        {
            // This file system cannot hold such a name, so there is nothing to refuse.
            return;
        }

        var act = () => PluginSignaturePayload.Build(folder.Path, GoldenKeyId);

        act.Should().Throw<InvalidDataException>();
    }

    /// <summary>
    /// Where a backslash is an ordinary file name character, <c>a\b</c> and <c>a/b</c> would both read as <c>a/b</c>; such a
    /// package is refused before anything is signed. Skipped where the file system cannot hold both names.
    /// </summary>
    [SkippableFact]
    public void Build_BackslashNameAndSlashPathSharingOnePath_Throws()
    {
        using var folder = new TempFolder();
        Skip.If(OperatingSystem.IsWindows(), "A backslash is a path separator here, so the two names cannot both exist.");
        folder.Write("a/b", "one");
        folder.Write("a\\b", "two");

        var act = () => PluginSignaturePayload.Build(folder.Path, GoldenKeyId);

        // The message names the duplicate path: a refusal for a control character or anything else would not match.
        act.Should().Throw<InvalidDataException>().WithMessage("*have the same path*");
    }
}
