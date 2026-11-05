namespace Hostpanel.Sdk.Packaging.Enums;

/// <summary>A way a plugin archive fails the archive guards. Values start at 1; 0 is not a rule.</summary>
public enum PluginArchiveRule
{
    /// <summary>The archive holds more entries than the limit allows.</summary>
    TooManyEntries = 1,

    /// <summary>The archive declares, or while unpacking turns out to hold, more uncompressed bytes than the limit allows.</summary>
    TooLarge = 2,

    /// <summary>An entry would be written outside the folder the archive is unpacked into (a path traversal entry).</summary>
    EntryOutsideFolder = 3,

    /// <summary>An entry could not be written into the unpack folder: a file and a folder of the same name clash, or the path is not writable.</summary>
    EntryNotWritable = 4,
}
