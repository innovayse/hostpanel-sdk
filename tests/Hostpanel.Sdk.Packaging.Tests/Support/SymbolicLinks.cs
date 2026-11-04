namespace Hostpanel.Sdk.Packaging.Tests.Support;

/// <summary>Makes symbolic links for the tests that need one, on a system that allows it.</summary>
public static class SymbolicLinks
{
    /// <summary>Makes a symbolic link to a file.</summary>
    /// <param name="linkPath">Where the link is made.</param>
    /// <param name="targetPath">The file it points to.</param>
    /// <returns><see langword="false"/> when the system or the account does not allow links, so the test has nothing to check.</returns>
    public static bool TryCreateFileLink(string linkPath, string targetPath)
    {
        try
        {
            File.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }

    /// <summary>Makes a symbolic link to a folder.</summary>
    /// <param name="linkPath">Where the link is made.</param>
    /// <param name="targetPath">The folder it points to.</param>
    /// <returns><see langword="false"/> when the system or the account does not allow links, so the test has nothing to check.</returns>
    public static bool TryCreateFolderLink(string linkPath, string targetPath)
    {
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }
}
