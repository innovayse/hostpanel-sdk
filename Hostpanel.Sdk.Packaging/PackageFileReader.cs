using Hostpanel.Sdk.Packaging.Enums;

namespace Hostpanel.Sdk.Packaging;

/// <summary>
/// Reads one file of an unpacked plugin package the safe way, once, for every check that needs a file's bytes (the icon,
/// the SSH key agent profile and its helpers): reached without a link, within a size cap and from one open handle. A link
/// would let a package read a file outside itself, so none on the way to the file is followed.
/// </summary>
internal static class PackageFileReader
{
    /// <summary>The separator of a package-relative path.</summary>
    private const char PackagePathSeparator = '/';

    /// <summary>How many bytes are read at a time; the read stops at the first chunk that passes the cap.</summary>
    private const int ReadChunkBytes = 8192;

    /// <summary>Reads a file of a package, refusing a link, a file over the cap and one that is not a regular file.</summary>
    /// <param name="root">The full path of the package folder, without a trailing separator.</param>
    /// <param name="relativePath">The package-relative path with forward slashes, already checked to hold plain segments only.</param>
    /// <param name="maxBytes">The largest size accepted, in bytes.</param>
    /// <returns>How the read ended and, when it succeeded, the bytes.</returns>
    internal static PackageFileRead Read(string root, string relativePath, long maxBytes)
    {
        var full = Path.GetFullPath(Path.Combine(root, relativePath));
        var located = Locate(root, relativePath, full);
        if (located != PackageFileOutcome.Read)
        {
            return new PackageFileRead(located, null);
        }

        try
        {
            // One handle for the size and the read, so the length checked is the length of the file that is read.
            using var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (!stream.CanSeek || stream.Length > maxBytes)
            {
                return new PackageFileRead(PackageFileOutcome.NotRegularFile, null);
            }

            // The path could have been swapped for a link between the first look and the open: look again, now that the
            // handle is held. The public API gives no final path of a handle, so the path is checked a second time.
            var relocated = Locate(root, relativePath, full);
            if (relocated != PackageFileOutcome.Read)
            {
                return new PackageFileRead(relocated, null);
            }

            var bytes = ReadBounded(stream, maxBytes);
            return bytes is null
                ? new PackageFileRead(PackageFileOutcome.NotRegularFile, null)
                : new PackageFileRead(PackageFileOutcome.Read, bytes);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // Removed between the look and the open: the one case of an unreadable file that is a missing one.
            return new PackageFileRead(PackageFileOutcome.Missing, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // A file that exists but cannot be read, or that is a device or a pipe, is not a readable regular file.
            return new PackageFileRead(PackageFileOutcome.Unreadable, null);
        }
    }

    /// <summary>
    /// Checks that the file exists and is reached without a link: no folder between the package folder and the file, and
    /// not the file, is a symbolic link or junction.
    /// </summary>
    /// <param name="root">The full path of the package folder.</param>
    /// <param name="relativePath">The package-relative path.</param>
    /// <param name="full">The full path of the file.</param>
    /// <returns><see cref="PackageFileOutcome.Read"/> when the file exists and nothing on the way is a link, otherwise why not.</returns>
    private static PackageFileOutcome Locate(string root, string relativePath, string full)
    {
        var segments = relativePath.Split(PackagePathSeparator);
        var current = root;
        foreach (var segment in segments.Take(segments.Length - 1))
        {
            current = Path.Combine(current, segment);
            var folder = new DirectoryInfo(current);
            if (folder.LinkTarget is not null)
            {
                return PackageFileOutcome.ReachedThroughLink;
            }

            if (!folder.Exists)
            {
                return PackageFileOutcome.Missing;
            }
        }

        var file = new FileInfo(full);
        if (file.LinkTarget is not null)
        {
            return PackageFileOutcome.IsLink;
        }

        return file.Exists ? PackageFileOutcome.Read : PackageFileOutcome.Missing;
    }

    /// <summary>Reads the stream to its end, stopping with <see langword="null"/> as soon as it passes the cap.</summary>
    /// <param name="stream">The open file.</param>
    /// <param name="maxBytes">The largest size accepted, in bytes.</param>
    /// <returns>The bytes, or <see langword="null"/> when the file is over <paramref name="maxBytes"/>.</returns>
    private static byte[]? ReadBounded(Stream stream, long maxBytes)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[ReadChunkBytes];
        int read;
        while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }
}
