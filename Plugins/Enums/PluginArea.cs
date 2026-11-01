namespace Innovayse.SDK.Plugins.Enums;

/// <summary>
/// The panel a plugin call or a slot belongs to, the same split as the <c>client</c> and <c>admin</c> members of
/// <c>ui</c> and <c>api</c> in <c>plugin.json</c>. Values start at 1; 0 is not an area.
/// </summary>
public enum PluginArea
{
    /// <summary>The client panel, used by a signed-in client or a member acting for one.</summary>
    Client = 1,

    /// <summary>The admin panel, used by an administrator.</summary>
    Admin = 2,
}
