using Hostpanel.Sdk.Packaging.Signing;

namespace Hostpanel.Sdk.Packaging.Tests.Support;

/// <summary>
/// The golden package the signature tests share: four files in folders, plus a root signature file that must not be listed.
/// It sits here, not in a test class, so a test class holds only tests.
/// </summary>
public static class GoldenPackage
{
    /// <summary>Writes the golden package into a folder.</summary>
    /// <param name="folder">The folder to fill.</param>
    public static void Write(TempFolder folder)
    {
        folder.Write("plugin.json", "{\"id\":\"demo\"}");
        folder.Write("lib/Demo.dll", "binary");
        folder.Write("assets/icon.svg", "<svg/>");
        folder.Write("Zed.txt", "zed");
        folder.Write(PluginSignatureFile.FileName, "{\"keyId\":\"demo-key\",\"signature\":\"ignored\"}");
    }
}
