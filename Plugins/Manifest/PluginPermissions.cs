namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>
/// The permission names a manifest may use besides a client permission name, in the <c>permission</c> member of a
/// client route, page or slot entry.
/// </summary>
public static class PluginPermissions
{
    /// <summary>
    /// The marker that admits every signed-in caller of the client panel, whatever permissions the caller holds.
    /// Compared exactly, so the spelling is this one.
    /// </summary>
    public const string Any = "Any";
}
