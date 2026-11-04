using System.Text;

namespace Hostpanel.Sdk.Packaging.Tests.Support;

/// <summary>A folder under the system temp folder that a test fills and that is deleted with the test.</summary>
public sealed class TempFolder : IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="TempFolder"/> class and creates the folder.</summary>
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hostpanel-packaging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>Gets the full path of the folder.</summary>
    public string Path { get; }

    /// <summary>Writes a text file (UTF-8, no byte order mark) below the folder, creating its folders.</summary>
    /// <param name="relativePath">The path below the folder, with forward slashes.</param>
    /// <param name="content">The file content.</param>
    /// <returns>The full path of the file.</returns>
    public string Write(string relativePath, string content)
    {
        return Write(relativePath, new UTF8Encoding(false).GetBytes(content));
    }

    /// <summary>Writes a file below the folder, creating its folders.</summary>
    /// <param name="relativePath">The path below the folder, with forward slashes.</param>
    /// <param name="content">The file content.</param>
    /// <returns>The full path of the file.</returns>
    public string Write(string relativePath, byte[] content)
    {
        var full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, content);
        return full;
    }

    /// <summary>Deletes the folder and everything in it.</summary>
    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
