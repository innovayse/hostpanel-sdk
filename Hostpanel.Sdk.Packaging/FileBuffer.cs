namespace Hostpanel.Sdk.Packaging;

/// <summary>The buffer every file read or written by this package uses, so the size is chosen once.</summary>
internal static class FileBuffer
{
    /// <summary>The size of the buffer that copies an archive entry and hashes a file, in bytes.</summary>
    internal const int Size = 81_920;
}
