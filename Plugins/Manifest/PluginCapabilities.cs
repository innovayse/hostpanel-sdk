namespace Hostpanel.Sdk.Plugins.Manifest;

/// <summary>
/// The capability names a plugin may list in the <c>capabilities</c> field of its manifest. The list lets the
/// admin and the client portal show what a plugin can do without activating it; it must agree with the
/// interfaces the plugin class really implements.
/// </summary>
public static class PluginCapabilities
{
    /// <summary>The payment plugin also implements <see cref="Hostpanel.Sdk.Plugins.StoredCredentials.IStoredCredentialPlugin"/>.</summary>
    public const string StoredCredentials = "storedCredentials";

    /// <summary>
    /// Any plugin type may declare it. It means the plugin class also implements
    /// <see cref="Hostpanel.Sdk.Plugins.Endpoints.IPluginEndpoints"/>, so the host serves the plugin's declared
    /// <c>api</c> routes and its <c>ui</c> pages, menus and slots. An <see cref="Hostpanel.Sdk.Plugins.Enums.PluginType.App"/>
    /// plugin is served without declaring it, and for it the host opens the client and admin routes, the pages and the
    /// slots. For any other plugin type the capability currently opens only the public callback routes
    /// (<c>api.callbacks</c>); its client and admin routes, pages and slots are not served yet.
    /// </summary>
    public const string Endpoints = "endpoints";
}
