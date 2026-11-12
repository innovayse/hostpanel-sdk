namespace Hostpanel.Sdk.Packaging;

/// <summary>The buffer of the streams that unpack an archive entry and hash a file of a package, so the size is chosen once.</summary>
internal static class FileBuffer
{
    /// <summary>The size of the buffer that copies an archive entry and hashes a file, in bytes.</summary>
    internal const int Size = 81_920;
}
