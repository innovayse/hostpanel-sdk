namespace Hostpanel.Sdk.Packaging;

/// <summary>What a path inside a plugin package looks like: always with forward slashes, whatever system it was written on.</summary>
internal static class PackagePath
{
    /// <summary>Gives a path inside a package its one spelling, with forward slashes for separators.</summary>
    /// <param name="path">A path from an archive entry name or relative to the package folder.</param>
    /// <returns>The path with every backslash written as a forward slash.</returns>
    internal static string Normalize(string path)
    {
        return path.Replace('\\', '/');
    }
}
