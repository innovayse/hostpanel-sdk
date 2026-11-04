using System.IO.Compression;
using Hostpanel.Sdk.Packaging.Archive;
using Hostpanel.Sdk.Packaging.Enums;
using Hostpanel.Sdk.Packaging.Tests.Support;

namespace Hostpanel.Sdk.Packaging.Tests.Archive;

/// <summary>Tests of <see cref="PluginArchiveGuard"/>: entry count, uncompressed size and path traversal.</summary>
public sealed class PluginArchiveGuardTests
{
    /// <summary>Limits generous enough for the small archives of these tests.</summary>
    private static readonly PluginArchiveLimits Roomy = new(MaxEntryCount: 100, MaxUncompressedBytes: 1_000_000);

    /// <summary>Builds an archive in memory from entry names and text contents.</summary>
    /// <param name="entries">The entries: name and content; a name ending in a slash is a folder.</param>
    /// <returns>The open archive, which owns its stream.</returns>
    private static ZipArchive Build(params (string Name, string Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var writer = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = writer.CreateEntry(name);
                using var target = entry.Open();
                var bytes = System.Text.Encoding.UTF8.GetBytes(content);
                target.Write(bytes, 0, bytes.Length);
            }
        }

        stream.Position = 0;
        return new ZipArchive(stream, ZipArchiveMode.Read);
    }

    /// <summary>An archive within both limits passes the declared check.</summary>
    [Fact]
    public void CheckDeclared_WithinLimits_Passes()
    {
        using var archive = Build(("plugin.json", "{}"), ("lib/a.dll", "abc"));

        PluginArchiveGuard.CheckDeclared(archive, Roomy).Should().BeNull();
    }

    /// <summary>More entries than the limit is refused, and exactly the limit is not.</summary>
    [Fact]
    public void CheckDeclared_TooManyEntries_IsRefused()
    {
        using var archive = Build(("a", "1"), ("b", "2"), ("c", "3"));

        PluginArchiveGuard.CheckDeclared(archive, new PluginArchiveLimits(3, 1000)).Should().BeNull();
        PluginArchiveGuard.CheckDeclared(archive, new PluginArchiveLimits(2, 1000))!.Rule.Should().Be(PluginArchiveRule.TooManyEntries);
    }

    /// <summary>A declared uncompressed size over the limit is refused before anything is unpacked.</summary>
    [Fact]
    public void CheckDeclared_TooLarge_IsRefused()
    {
        using var archive = Build(("a", new string('x', 600)), ("b", new string('y', 600)));

        PluginArchiveGuard.CheckDeclared(archive, new PluginArchiveLimits(10, 1200)).Should().BeNull();
        PluginArchiveGuard.CheckDeclared(archive, new PluginArchiveLimits(10, 1199))!.Rule.Should().Be(PluginArchiveRule.TooLarge);
    }

    /// <summary>The real byte count is held to the limit too: the count passes it only when it is greater.</summary>
    [Fact]
    public void IsOverSizeLimit_IsTrueOnlyAboveTheLimit()
    {
        var limits = new PluginArchiveLimits(10, 100);

        PluginArchiveGuard.IsOverSizeLimit(100, limits).Should().BeFalse();
        PluginArchiveGuard.IsOverSizeLimit(101, limits).Should().BeTrue();
    }

    /// <summary>Entries that stay inside the folder resolve, with either kind of separator.</summary>
    /// <param name="entry">The entry name.</param>
    [Theory]
    [InlineData("plugin.json")]
    [InlineData("lib/a.dll")]
    [InlineData("lib\\a.dll")]
    [InlineData("a/b/../c.txt")]
    public void TryResolveEntry_InsideTheFolder_Resolves(string entry)
    {
        using var folder = new TempFolder();

        PluginArchiveGuard.TryResolveEntry(folder.Path, entry, out var target).Should().BeTrue();
        target.Should().StartWith(Path.GetFullPath(folder.Path));
    }

    /// <summary>Entries that would leave the folder are refused: parent folders, back-slash parents, absolute paths, a sibling that shares the prefix.</summary>
    /// <param name="entry">The entry name.</param>
    [Theory]
    [InlineData("../evil.dll")]
    [InlineData("a/../../evil.dll")]
    [InlineData("..\\evil.dll")]
    [InlineData("/etc/evil")]
    [InlineData("a\0b")]
    public void TryResolveEntry_OutsideTheFolder_IsRefused(string entry)
    {
        using var folder = new TempFolder();

        PluginArchiveGuard.TryResolveEntry(folder.Path, entry, out _).Should().BeFalse();
    }

    /// <summary>A path that only shares the folder's name as a prefix is outside it.</summary>
    [Fact]
    public void TryResolveEntry_SiblingWithSamePrefix_IsRefused()
    {
        using var folder = new TempFolder();
        var sibling = "../" + new DirectoryInfo(folder.Path).Name + "-other/evil.dll";

        PluginArchiveGuard.TryResolveEntry(folder.Path, sibling, out _).Should().BeFalse();
    }

    /// <summary>An archive with a traversal entry is refused and nothing is written outside the folder.</summary>
    [Fact]
    public void Extract_TraversalEntry_IsRefusedAndWritesNothingOutside()
    {
        using var parent = new TempFolder();
        var target = Path.Combine(parent.Path, "unpack");
        using var archive = Build(("plugin.json", "{}"), ("../escaped.txt", "x"));

        var violation = PluginArchiveGuard.Extract(archive, target, Roomy);

        violation!.Rule.Should().Be(PluginArchiveRule.EntryOutsideFolder);
        File.Exists(Path.Combine(parent.Path, "escaped.txt")).Should().BeFalse();
    }

    /// <summary>A clean archive is unpacked with its folders and file content.</summary>
    [Fact]
    public void Extract_CleanArchive_WritesTheFiles()
    {
        using var parent = new TempFolder();
        var target = Path.Combine(parent.Path, "unpack");
        using var archive = Build(("plugin.json", "{}"), ("lib/", string.Empty), ("lib/a.dll", "abc"));

        PluginArchiveGuard.Extract(archive, target, Roomy).Should().BeNull();

        File.ReadAllText(Path.Combine(target, "plugin.json")).Should().Be("{}");
        File.ReadAllText(Path.Combine(target, "lib", "a.dll")).Should().Be("abc");
    }

    /// <summary>An archive over the declared limit is refused before any file is written.</summary>
    [Fact]
    public void Extract_OverTheDeclaredLimit_WritesNothing()
    {
        using var parent = new TempFolder();
        var target = Path.Combine(parent.Path, "unpack");
        using var archive = Build(("a", new string('x', 500)));

        var violation = PluginArchiveGuard.Extract(archive, target, new PluginArchiveLimits(10, 100));

        violation!.Rule.Should().Be(PluginArchiveRule.TooLarge);
        Directory.Exists(target).Should().BeFalse();
    }

    /// <summary>A path that differs from the folder only in case is a different path and is refused, never read as the folder.</summary>
    [Fact]
    public void TryResolveEntry_CaseDifferentSibling_IsRefused()
    {
        using var folder = new TempFolder();
        var sibling = "../" + new DirectoryInfo(folder.Path).Name.ToUpperInvariant() + "/evil.dll";

        PluginArchiveGuard.TryResolveEntry(folder.Path, sibling, out _).Should().BeFalse();
    }

    /// <summary>Declared sizes whose sum passes the range of a long are refused instead of wrapping round to a small number.</summary>
    [Fact]
    public void CheckDeclared_SizesThatOverflowTheirSum_AreTooLarge()
    {
        using var archive = CraftedArchive.Create(
            ("a", (ulong)long.MaxValue, Array.Empty<byte>()), ("b", (ulong)long.MaxValue, Array.Empty<byte>()),
            ("c", (ulong)long.MaxValue, Array.Empty<byte>()));

        var violation = PluginArchiveGuard.CheckDeclared(archive, new PluginArchiveLimits(10, long.MaxValue));

        violation!.Rule.Should().Be(PluginArchiveRule.TooLarge);
    }

    /// <summary>A file where a folder is needed is a refusal of the archive, not an exception.</summary>
    [Fact]
    public void Extract_FileAndFolderOfOneName_IsNotWritable()
    {
        using var parent = new TempFolder();
        var target = Path.Combine(parent.Path, "unpack");
        using var archive = Build(("lib", "a file"), ("lib/a.dll", "inside a folder of the same name"));

        var violation = PluginArchiveGuard.Extract(archive, target, Roomy);

        violation!.Rule.Should().Be(PluginArchiveRule.EntryNotWritable);
        violation.Detail.Should().NotContain(parent.Path);
    }

    /// <summary>A folder entry that ends at the unpack folder itself is accepted and the rest of the archive is unpacked.</summary>
    /// <param name="folderEntry">The folder entry name.</param>
    [Theory]
    [InlineData("./")]
    [InlineData("dir/..")]
    [InlineData("dir/../")]
    [InlineData("dir\\..\\")]
    public void Extract_FolderEntryResolvingToTheRoot_IsAccepted(string folderEntry)
    {
        using var parent = new TempFolder();
        var target = Path.Combine(parent.Path, "unpack");
        using var archive = Build((folderEntry, string.Empty), ("plugin.json", "{}"));

        PluginArchiveGuard.Extract(archive, target, Roomy).Should().BeNull();

        File.ReadAllText(Path.Combine(target, "plugin.json")).Should().Be("{}");
    }

    /// <summary>The real byte count is held to the limit while unpacking, even when the central directory declares less.</summary>
    [Fact]
    public void Extract_EntryHoldingMoreThanItDeclares_IsTooLarge()
    {
        using var parent = new TempFolder();
        var target = Path.Combine(parent.Path, "unpack");
        using var archive = CraftedArchive.Create(("a.bin", 10, new byte[500]));

        PluginArchiveGuard.CheckDeclared(archive, new PluginArchiveLimits(10, 100)).Should().BeNull();
        var violation = PluginArchiveGuard.Extract(archive, target, new PluginArchiveLimits(10, 100));

        violation!.Rule.Should().Be(PluginArchiveRule.TooLarge);
    }
}
