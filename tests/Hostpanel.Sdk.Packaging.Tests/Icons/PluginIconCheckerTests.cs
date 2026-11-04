using Hostpanel.Sdk.Packaging.Enums;
using Hostpanel.Sdk.Packaging.Icons;
using Hostpanel.Sdk.Packaging.Tests.Support;
using static Hostpanel.Sdk.Packaging.Tests.Icons.IconFixtures;

namespace Hostpanel.Sdk.Packaging.Tests.Icons;

/// <summary>Tests of <see cref="PluginIconChecker"/>: path rule, size cap, PNG signature, read errors, details and links.</summary>
public sealed class PluginIconCheckerTests
{
    /// <summary>A manifest that names no icon has nothing to check.</summary>
    /// <param name="iconPath">The manifest value.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Check_NoIcon_IsNotAnError(string? iconPath)
    {
        using var folder = new TempFolder();

        var result = PluginIconChecker.Check(folder.Path, iconPath);

        result.Icon.Should().BeNull();
        result.Violation.Should().BeNull();
    }

    /// <summary>A good SVG is accepted with its media type, its bytes and a strong tag.</summary>
    [Fact]
    public void Check_GoodSvg_IsAccepted()
    {
        var result = CheckFile("icon.svg", GoodSvg);

        result.Violation.Should().BeNull();
        result.Icon!.ContentType.Should().Be("image/svg+xml");
        result.Icon.Content.Should().Equal(System.Text.Encoding.UTF8.GetBytes(GoodSvg));
        result.Icon.ETag.Should().MatchRegex("^\"[0-9a-f]{32}\"$");
    }

    /// <summary>A package folder given with a trailing separator is the same folder.</summary>
    [Fact]
    public void Check_PackageFolderWithTrailingSeparator_IsAccepted()
    {
        using var folder = new TempFolder();
        folder.Write("icon.svg", GoodSvg);

        var result = PluginIconChecker.Check(folder.Path + Path.DirectorySeparatorChar, "icon.svg");

        result.Violation.Should().BeNull();
        result.Icon.Should().NotBeNull();
    }

    /// <summary>A good PNG is accepted, and a file in a folder is reached.</summary>
    [Fact]
    public void Check_GoodPng_InAFolder_IsAccepted()
    {
        using var folder = new TempFolder();
        folder.Write("assets/logo.png", GoodPng);

        var result = PluginIconChecker.Check(folder.Path, "assets/logo.png");

        result.Violation.Should().BeNull();
        result.Icon!.ContentType.Should().Be("image/png");
    }

    /// <summary>The same content gives the same tag, and a different content a different one.</summary>
    [Fact]
    public void Check_ETag_FollowsTheContent()
    {
        var first = CheckFile("icon.svg", GoodSvg).Icon!.ETag;
        var again = CheckFile("icon.svg", GoodSvg).Icon!.ETag;
        var other = CheckFile("icon.svg", GoodSvg.Replace("#fff", "#000")).Icon!.ETag;

        again.Should().Be(first);
        other.Should().NotBe(first);
    }

    /// <summary>A path that is not a plain package-relative image path is invalid, before any file is touched.</summary>
    /// <param name="path">The manifest value.</param>
    [Theory]
    [InlineData("../icon.svg")]
    [InlineData("a/../icon.svg")]
    [InlineData("./icon.svg")]
    [InlineData("a/./icon.svg")]
    [InlineData("/icon.svg")]
    [InlineData("a//icon.svg")]
    [InlineData("Icon.svg")]
    [InlineData("icon.SVG")]
    [InlineData("icon.Png")]
    [InlineData("icon.svgz")]
    [InlineData("icon.svg/")]
    [InlineData(".hidden/icon.svg")]
    [InlineData("icon.gif")]
    [InlineData("icon.svg\n")]
    [InlineData("a\\icon.svg")]
    [InlineData("..\\icon.svg")]
    [InlineData("C:/icon.svg")]
    [InlineData("C:\\icon.svg")]
    [InlineData("c:icon.svg")]
    [InlineData("https://example.org/icon.svg")]
    public void Check_UnsafePath_IsInvalid(string path)
    {
        using var folder = new TempFolder();

        var violation = PluginIconChecker.Check(folder.Path, path).Violation!;

        violation.Rule.Should().Be(PluginIconRule.Invalid);
        violation.Detail.Should().StartWith("icon path ");
    }

    /// <summary>A refused manifest string is never echoed in the detail, whatever it holds.</summary>
    [Fact]
    public void Check_RefusedPath_IsNotEchoedInTheDetail()
    {
        using var folder = new TempFolder();

        var detail = PluginIconChecker.Check(folder.Path, "Secret\u0007Token\nforged-log-line.svg").Violation!.Detail;

        detail.Should().NotContain("Secret").And.NotContain("forged").And.NotContain("\n").And.NotContain("\u0007");
        detail.Should().NotContain(folder.Path);
    }

    /// <summary>An icon the manifest names but the package does not hold is missing, not invalid.</summary>
    [Fact]
    public void Check_FileNotInPackage_IsMissing()
    {
        using var folder = new TempFolder();

        PluginIconChecker.Check(folder.Path, "icon.svg").Violation!.Rule.Should().Be(PluginIconRule.Missing);
        PluginIconChecker.Check(folder.Path, "nofolder/icon.svg").Violation!.Rule.Should().Be(PluginIconRule.Missing);
    }

    /// <summary>A file that exists but cannot be opened is invalid, not missing: only an absent file is missing.</summary>
    [Fact]
    public void Check_FileThatCannotBeOpened_IsInvalidNotMissing()
    {
        using var folder = new TempFolder();
        var path = folder.Write("icon.svg", GoodSvg);

        // An exclusive handle held by someone else makes the open fail with an I/O error.
        using var held = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var result = PluginIconChecker.Check(folder.Path, "icon.svg");

        result.Icon.Should().BeNull();
        result.Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        result.Violation.Detail.Should().NotContain(folder.Path).And.Contain("icon.svg");
    }

    /// <summary>The detail names the package-relative path and never the folder the package is in.</summary>
    [Fact]
    public void Check_Detail_NeverHoldsTheAbsolutePath()
    {
        using var folder = new TempFolder();

        var detail = PluginIconChecker.Check(folder.Path, "icon.svg").Violation!.Detail;

        detail.Should().Contain("icon.svg").And.NotContain(folder.Path);
    }

    /// <summary>A file of exactly the size cap is accepted, and one byte more is refused.</summary>
    [Fact]
    public void Check_SizeBoundary_CapPassesAndOneMoreFails()
    {
        var cap = PluginIconChecker.MaxIconBytes;
        var svgAtCap = GoodSvg + new string(' ', (int)cap - GoodSvg.Length);
        var pngAtCap = Pad(GoodPng, cap);

        CheckFile("icon.svg", svgAtCap).Violation.Should().BeNull();
        CheckBytes("icon.png", pngAtCap).Violation.Should().BeNull();
        CheckFile("icon.svg", svgAtCap + " ").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        CheckBytes("icon.png", Pad(GoodPng, cap + 1)).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>A file far over the size cap is invalid.</summary>
    [Fact]
    public void Check_OverTheSizeCap_IsInvalid()
    {
        var big = GoodSvg + new string(' ', (int)PluginIconChecker.MaxIconBytes);

        CheckFile("icon.svg", big).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>A file that says it is a PNG but is not, is invalid.</summary>
    [Fact]
    public void Check_FakePng_IsInvalid()
    {
        CheckFile("icon.png", "<svg xmlns=\"http://www.w3.org/2000/svg\"/>").Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>A PNG whose first chunk is <c>IDAT</c>, not <c>IHDR</c>, is invalid.</summary>
    [Fact]
    public void Check_PngWithIdatInsteadOfIhdr_IsInvalid()
    {
        var bytes = (byte[])GoodPng.Clone();
        System.Text.Encoding.ASCII.GetBytes("IDAT").CopyTo(bytes, 12);

        CheckBytes("icon.png", bytes).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>A PNG shorter than the signature plus the first chunk header is invalid, and 16 bytes is enough.</summary>
    [Fact]
    public void Check_ShortPng_IsInvalidUnderSixteenBytes()
    {
        CheckBytes("icon.png", GoodPng[..15]).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        CheckBytes("icon.png", GoodPng[..8]).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        CheckBytes("icon.png", GoodPng[..4]).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        CheckBytes("icon.png", []).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        CheckBytes("icon.png", GoodPng[..16]).Violation.Should().BeNull();
    }

    /// <summary>A PNG with a wrong signature is invalid.</summary>
    [Fact]
    public void Check_PngWithWrongSignature_IsInvalid()
    {
        var bytes = (byte[])GoodPng.Clone();
        bytes[0] = 0x00;

        CheckBytes("icon.png", bytes).Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }

    /// <summary>An icon that is a symbolic link is refused, whatever it points at.</summary>
    [SkippableFact]
    public void Check_IconThatIsALink_IsInvalid()
    {
        using var folder = new TempFolder();
        using var outside = new TempFolder();
        var target = outside.Write("real.svg", GoodSvg);
        Skip.IfNot(
            SymbolicLinks.TryCreateFileLink(Path.Combine(folder.Path, "icon.svg"), target),
            "This account may not create symbolic links, so there is no link to refuse.");

        var result = PluginIconChecker.Check(folder.Path, "icon.svg");

        result.Icon.Should().BeNull();
        result.Violation!.Rule.Should().Be(PluginIconRule.Invalid);
        result.Violation.Detail.Should().NotContain(folder.Path).And.NotContain(outside.Path);
    }

    /// <summary>An icon reached through a folder that is a symbolic link is refused.</summary>
    [SkippableFact]
    public void Check_IconBehindALinkedFolder_IsInvalid()
    {
        using var folder = new TempFolder();
        using var outside = new TempFolder();
        outside.Write("real.svg", GoodSvg);
        Skip.IfNot(
            SymbolicLinks.TryCreateFolderLink(Path.Combine(folder.Path, "assets"), outside.Path),
            "This account may not create symbolic links, so there is no link to refuse.");

        var result = PluginIconChecker.Check(folder.Path, "assets/real.svg");

        result.Icon.Should().BeNull();
        result.Violation!.Rule.Should().Be(PluginIconRule.Invalid);
    }
}
