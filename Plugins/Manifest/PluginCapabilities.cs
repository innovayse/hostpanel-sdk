namespace Innovayse.SDK.Plugins.Manifest;

/// <summary>
/// The capability names a plugin may list in the <c>capabilities</c> field of its manifest. The list lets the
/// admin and the client portal show what a plugin can do without activating it; it must agree with the
/// interfaces the plugin class really implements.
/// </summary>
public static class PluginCapabilities
{
    /// <summary>The payment plugin also implements <see cref="Innovayse.SDK.Plugins.StoredCredentials.IStoredCredentialPlugin"/>.</summary>
    public const string StoredCredentials = "storedCredentials";

    /// <summary>
    /// Any plugin type may declare it. It means the plugin class also implements
    /// <see cref="Innovayse.SDK.Plugins.Endpoints.IPluginEndpoints"/>, so the host serves the plugin's declared
    /// <c>api</c> routes and its <c>ui</c> pages, menus and slots. An <see cref="Innovayse.SDK.Plugins.Enums.PluginType.App"/>
    /// plugin is served without declaring it.
    /// </summary>
    public const string Endpoints = "endpoints";
}
