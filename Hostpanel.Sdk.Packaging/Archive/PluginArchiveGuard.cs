using System.IO.Compression;
using Hostpanel.Sdk.Packaging.Enums;

namespace Hostpanel.Sdk.Packaging.Archive;

/// <summary>
/// The guards a plugin archive passes before and while it is unpacked: the entry count, the uncompressed size (the
/// declared size up front, the real byte count while unpacking, because the declared size is only what the archive
/// claims) and path traversal (an entry must land inside the folder it is unpacked into).
/// </summary>
public static class PluginArchiveGuard
{
    /// <summary>Checks the entry count and the declared uncompressed size, before anything is unpacked.</summary>
    /// <param name="archive">The open archive.</param>
    /// <param name="limits">The limits.</param>
    /// <returns>The guard the archive fails, or <see langword="null"/> when it is within both limits.</returns>
    public static PluginArchiveViolation? CheckDeclared(ZipArchive archive, PluginArchiveLimits limits)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(limits);

        if (archive.Entries.Count > limits.MaxEntryCount)
        {
            return new PluginArchiveViolation(
                PluginArchiveRule.TooManyEntries, $"the archive holds {archive.Entries.Count} entries, the limit is {limits.MaxEntryCount}");
        }

        // Summed with an early exit: a sum of lengths an archive declares can pass the range of a long, and a sum that wrapped
        // round would read as a small number. The comparison is written so that it cannot overflow itself.
        long declared = 0;
        foreach (var entry in archive.Entries)
        {
            if (entry.Length > limits.MaxUncompressedBytes - declared)
            {
                return new PluginArchiveViolation(
                    PluginArchiveRule.TooLarge, $"the archive declares more than {limits.MaxUncompressedBytes} uncompressed bytes");
            }

            declared += entry.Length;
        }

        return null;
    }

    /// <summary>Whether the bytes written so far pass the uncompressed size limit.</summary>
    /// <param name="writtenBytes">The bytes written over all entries so far.</param>
    /// <param name="limits">The limits.</param>
    /// <returns><see langword="true"/> when the count is over <see cref="PluginArchiveLimits.MaxUncompressedBytes"/> and unpacking must stop.</returns>
    public static bool IsOverSizeLimit(long writtenBytes, PluginArchiveLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        return writtenBytes > limits.MaxUncompressedBytes;
    }

    /// <summary>
    /// Resolves where an entry would be written and checks that it stays inside the folder. A back-slash is read as a
    /// separator (an archive made on another system may use it), and an absolute or <c>..</c> path that leaves the folder
    /// is refused.
    /// </summary>
    /// <param name="contentDir">The folder the archive is unpacked into.</param>
    /// <param name="entryFullName">The entry's name inside the archive, with its folders.</param>
    /// <param name="targetPath">The full path the entry would be written to; set even when the entry is refused.</param>
    /// <returns><see langword="true"/> when the target is inside <paramref name="contentDir"/> or is the folder itself (<c>./</c>, <c>dir/..</c>), which holds nothing to write.</returns>
    public static bool TryResolveEntry(string contentDir, string entryFullName, out string targetPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentDir);
        ArgumentNullException.ThrowIfNull(entryFullName);

        return Resolve(contentDir, entryFullName, out targetPath, out _);
    }

    /// <summary>Resolves an entry's target and tells whether it is the folder itself, which is where <c>./</c> and <c>dir/..</c> end.</summary>
    /// <param name="contentDir">The folder the archive is unpacked into.</param>
    /// <param name="entryFullName">The entry's name inside the archive.</param>
    /// <param name="targetPath">The full path the entry would be written to; empty when the name cannot be resolved.</param>
    /// <param name="isFolderItself">Whether the target is <paramref name="contentDir"/> itself rather than something inside it.</param>
    /// <returns><see langword="true"/> when the target is inside <paramref name="contentDir"/> or is the folder itself.</returns>
    private static bool Resolve(string contentDir, string entryFullName, out string targetPath, out bool isFolderItself)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(contentDir));
        targetPath = string.Empty;
        isFolderItself = false;
        try
        {
            targetPath = Path.GetFullPath(Path.Combine(contentDir, entryFullName.Replace('\\', '/')));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // A name the file system cannot hold (a NUL, a reserved device name, a path that is too long) is never a safe target.
            return false;
        }

        // Compared exactly: a path that differs from the folder only in case is a different path on a case-sensitive file
        // system, and a name that lands beside the folder must never pass because it looks like the folder.
        isFolderItself = string.Equals(Path.TrimEndingDirectorySeparator(targetPath), root, StringComparison.Ordinal);
        return isFolderItself || targetPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    /// <summary>
    /// Unpacks an archive into a folder under every guard: the declared limits first, then each entry's target, then the
    /// real byte count while copying. Files are always written as regular files, never as links. On a refusal files
    /// written so far stay where they are; unpack into a staging folder and discard it.
    /// </summary>
    /// <param name="archive">The open archive.</param>
    /// <param name="contentDir">The folder to unpack into; created when missing.</param>
    /// <param name="limits">The limits.</param>
    /// <returns>The guard the archive fails, or <see langword="null"/> when everything was unpacked.</returns>
    public static PluginArchiveViolation? Extract(ZipArchive archive, string contentDir, PluginArchiveLimits limits)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentException.ThrowIfNullOrEmpty(contentDir);
        ArgumentNullException.ThrowIfNull(limits);

        var declared = CheckDeclared(archive, limits);
        if (declared is not null)
        {
            return declared;
        }

        Directory.CreateDirectory(contentDir);
        var buffer = new byte[FileBuffer.Size];
        long written = 0;

        foreach (var entry in archive.Entries)
        {
            if (!Resolve(contentDir, entry.FullName, out var targetPath, out var isFolderItself))
            {
                return new PluginArchiveViolation(PluginArchiveRule.EntryOutsideFolder, "an entry would be written outside the unpack folder");
            }

            // A folder entry holds nothing to write, and a name that ends at the unpack folder itself (./, dir/..) has no file to be.
            if (isFolderItself || IsFolderEntry(entry))
            {
                continue;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                using var source = entry.Open();
                using var target = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, FileBuffer.Size);
                int read;
                while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                {
                    written += read;
                    if (IsOverSizeLimit(written, limits))
                    {
                        return new PluginArchiveViolation(
                            PluginArchiveRule.TooLarge, $"the archive unpacks to more than {limits.MaxUncompressedBytes} bytes");
                    }

                    target.Write(buffer, 0, read);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A file where a folder is needed (or the reverse), or a path the account may not write: a refusal of this archive.
                // The type only: an I/O message carries the absolute path of the unpack folder.
                return new PluginArchiveViolation(
                    PluginArchiveRule.EntryNotWritable, $"an entry could not be written to the unpack folder ({ex.GetType().Name})");
            }
        }

        return null;
    }

    /// <summary>Whether an entry names a folder: its name ends in a separator of either kind.</summary>
    /// <param name="entry">The archive entry.</param>
    /// <returns><see langword="true"/> for a folder entry.</returns>
    private static bool IsFolderEntry(ZipArchiveEntry entry)
    {
        return entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\');
    }
}
