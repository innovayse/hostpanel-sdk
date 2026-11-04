namespace Hostpanel.Sdk.Plugins.Enums;

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

    /// <summary>
    /// A callback: a request from outside the panels, for example a gateway notifying the plugin, which carries no
    /// panel session. The host authenticates nothing for it, so the plugin must verify the sender itself (a signature
    /// over <see cref="Endpoints.PluginCall.Body"/>, for instance) before it acts.
    /// </summary>
    Callback = 3,
}
