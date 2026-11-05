namespace Hostpanel.Sdk.Packaging.Enums;

/// <summary>A way a plugin icon fails the icon check. Values start at 1; 0 is not a rule.</summary>
public enum PluginIconRule
{
    /// <summary>The manifest names an icon file that is not in the package, or that cannot be read.</summary>
    Missing = 1,

    /// <summary>
    /// The icon is not acceptable: the path is not a plain package-relative image path, a link is on the way, the file is
    /// over the size cap, or its content is not the image it claims to be or carries active content.
    /// </summary>
    Invalid = 2,
}
