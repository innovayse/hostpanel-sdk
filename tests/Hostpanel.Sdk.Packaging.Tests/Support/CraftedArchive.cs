using System.IO.Compression;
using System.Text;

namespace Hostpanel.Sdk.Packaging.Tests.Support;

/// <summary>
/// Builds a zip archive byte by byte, so a test can make an archive whose central directory says something other than what
/// the entries hold: a size that is far over a limit, or one that is smaller than the data. A real archiver never writes
/// either.
/// </summary>
public static class CraftedArchive
{
    /// <summary>The marker that stands in for a size held in the Zip64 extra field.</summary>
    private const uint Zip64Marker = 0xFFFFFFFF;

    /// <summary>The signature of a local file header.</summary>
    private const uint LocalHeaderSignature = 0x04034b50;

    /// <summary>The signature of a central directory file header.</summary>
    private const uint CentralHeaderSignature = 0x02014b50;

    /// <summary>The signature of the end of central directory record.</summary>
    private const uint EndSignature = 0x06054b50;

    /// <summary>The id of the Zip64 extended information extra field.</summary>
    private const ushort Zip64ExtraId = 0x0001;

    /// <summary>The zip specification version that reads Zip64 fields.</summary>
    private const ushort Version = 45;

    /// <summary>Builds an archive of stored (uncompressed) entries, each declaring a size of its own.</summary>
    /// <param name="entries">
    /// The entries: the name, the uncompressed size the central directory declares, and the bytes actually stored. A size
    /// over 4 GiB is written as Zip64; a size below the stored length makes the archive lie about its content.
    /// </param>
    /// <returns>The open archive, which owns its stream.</returns>
    public static ZipArchive Create(params (string Name, ulong DeclaredSize, byte[] Data)[] entries)
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            var offsets = new List<uint>();
            foreach (var (name, declaredSize, data) in entries)
            {
                offsets.Add((uint)stream.Position);
                WriteLocalHeader(writer, name, Math.Min(declaredSize, uint.MaxValue - 1UL), data);
            }

            var directoryStart = (uint)stream.Position;
            for (var i = 0; i < entries.Length; i++)
            {
                WriteCentralHeader(writer, entries[i].Name, entries[i].DeclaredSize, entries[i].Data.Length, offsets[i]);
            }

            var directorySize = (uint)stream.Position - directoryStart;
            writer.Write(EndSignature);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)entries.Length);
            writer.Write((ushort)entries.Length);
            writer.Write(directorySize);
            writer.Write(directoryStart);
            writer.Write((ushort)0);
        }

        stream.Position = 0;
        return new ZipArchive(stream, ZipArchiveMode.Read);
    }

    /// <summary>Writes a local file header and the stored bytes of one entry.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="name">The entry name.</param>
    /// <param name="declaredSize">The uncompressed size to write, which fits 32 bits.</param>
    /// <param name="data">The stored bytes.</param>
    private static void WriteLocalHeader(BinaryWriter writer, string name, ulong declaredSize, byte[] data)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        writer.Write(LocalHeaderSignature);
        writer.Write(Version);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(0u);
        writer.Write((uint)data.Length);
        writer.Write((uint)declaredSize);
        writer.Write((ushort)nameBytes.Length);
        writer.Write((ushort)0);
        writer.Write(nameBytes);
        writer.Write(data);
    }

    /// <summary>Writes a central directory header, with a Zip64 extra field when the declared size needs one.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="name">The entry name.</param>
    /// <param name="declaredSize">The uncompressed size to declare.</param>
    /// <param name="storedLength">The length of the stored bytes, which is the compressed size of a stored entry.</param>
    /// <param name="localHeaderOffset">Where the entry's local header starts.</param>
    private static void WriteCentralHeader(BinaryWriter writer, string name, ulong declaredSize, int storedLength, uint localHeaderOffset)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var needsZip64 = declaredSize >= Zip64Marker;
        writer.Write(CentralHeaderSignature);
        writer.Write(Version);
        writer.Write(Version);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(0u);
        writer.Write(needsZip64 ? Zip64Marker : (uint)storedLength);
        writer.Write(needsZip64 ? Zip64Marker : (uint)declaredSize);
        writer.Write((ushort)nameBytes.Length);
        writer.Write((ushort)(needsZip64 ? 20 : 0));
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write(0u);
        writer.Write(localHeaderOffset);
        writer.Write(nameBytes);
        if (needsZip64)
        {
            // The extra field holds the uncompressed size first, then the compressed size.
            writer.Write(Zip64ExtraId);
            writer.Write((ushort)16);
            writer.Write(declaredSize);
            writer.Write((ulong)storedLength);
        }
    }
}
