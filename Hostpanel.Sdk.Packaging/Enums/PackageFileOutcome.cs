namespace Hostpanel.Sdk.Packaging.Enums;

/// <summary>How reading one file of a plugin package ended. Values start at 1; 0 is not an outcome.</summary>
internal enum PackageFileOutcome
{
    /// <summary>The file was read whole, within the size cap.</summary>
    Read = 1,

    /// <summary>The file or a folder on the way does not exist, or vanished while it was read.</summary>
    Missing = 2,

    /// <summary>A folder between the package folder and the file is a symbolic link or junction.</summary>
    ReachedThroughLink = 3,

    /// <summary>The file itself is a symbolic link.</summary>
    IsLink = 4,

    /// <summary>The file is over the size cap, or is not a seekable regular file.</summary>
    NotRegularFile = 5,

    /// <summary>The file exists but cannot be read, for example for lack of permission, or it is a device or a pipe.</summary>
    Unreadable = 6,
}
