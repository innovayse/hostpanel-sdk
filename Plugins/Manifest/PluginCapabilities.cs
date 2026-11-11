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

    /// <summary>The provisioning plugin also implements <see cref="Hostpanel.Sdk.Plugins.Provisioning.IProvisioningPanelPlugin"/>, the account operations of a hosting control panel.</summary>
    public const string ProvisioningPanel = "provisioningPanel";

    /// <summary>
    /// The provisioning plugin also implements <see cref="Hostpanel.Sdk.Plugins.VirtualServers.IVirtualServerPlugin"/>, the
    /// operations of a virtual server or cloud platform. The plugin type stays <see cref="Hostpanel.Sdk.Plugins.Enums.PluginType.Provisioning"/>:
    /// a virtual server is a service provisioned on a server the host knows, like a hosting account.
    /// </summary>
    public const string VirtualServer = "virtualServer";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarOperationsPlugin"/>, the domain operations of a registrar.</summary>
    public const string RegistrarOperations = "registrarOperations";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarEmailForwardingPlugin"/>, e-mail forwarding for its domains.</summary>
    public const string RegistrarEmailForwarding = "registrarEmailForwarding";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarDnsManagementPlugin"/>, the DNS management switch.</summary>
    public const string RegistrarDnsManagement = "registrarDnsManagement";

    /// <summary>The registrar plugin also implements <see cref="Hostpanel.Sdk.Plugins.Registrar.IRegistrarWhoisPlugin"/>, the WHOIS lookup.</summary>
    public const string RegistrarWhois = "registrarWhois";

    /// <summary>The virtual server plugin also implements <see cref="Hostpanel.Sdk.Plugins.VirtualServers.IVirtualServerListPlugin"/>, the listing of the servers a platform holds, for reconciliation.</summary>
    public const string VirtualServerList = "virtualServerList";

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
