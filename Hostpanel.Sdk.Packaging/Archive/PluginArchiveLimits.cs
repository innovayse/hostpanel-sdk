namespace Hostpanel.Sdk.Packaging.Archive;

/// <summary>The size limits a plugin archive is held to.</summary>
/// <param name="MaxEntryCount">The most entries (files and folders) the archive may hold.</param>
/// <param name="MaxUncompressedBytes">The most bytes the archive may hold once unpacked, counted over all entries.</param>
public sealed record PluginArchiveLimits(int MaxEntryCount, long MaxUncompressedBytes);
